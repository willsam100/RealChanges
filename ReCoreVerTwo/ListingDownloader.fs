module ListingDownloader
open FSharp.Data
open System
open System.IO

type SimpleListing = {
    ListingId: int
    Price: string
    Title: string
}

type FullListing = {
    Listing: SimpleListing
    DateAdded: DateTime
    Views: int
    Image: Uri option
    WasRemovedOrSold: bool
}
    
let AsyncQueryListing (logger: string -> unit) listingId = 
    async {
        
        let tableWithPrice = "ListingAttributes"
        let tableRows = "tr"

        let isExpired (body: HtmlDocument) = 
            body.Descendants ["h1"]
            |> Seq.map (fun (x) -> x.InnerText())
            |> Seq.map (fun x -> x = "Sorry, this classified has expired.")
            |> Seq.fold (||) false
            
        let wasRemoved (body: HtmlDocument) = 
            body.Descendants ["p"]
            |> Seq.map (fun (x) -> x.InnerText())
            |> Seq.map (fun x -> x = "This listing was withdrawn by the administrator")
            |> Seq.fold (||) false
            

        let header (body: HtmlDocument) = 
            body.Descendants ["h1"] 
            |> Seq.choose (fun x -> 
               x.TryGetAttribute("id") |> Option.map (fun a -> x.InnerText())) 
            |> Seq.map (fun x -> x.Trim())
            |> Seq.tryHead

        let askingPrice (body: HtmlDocument) = 
            body.Descendants ["table"] 
            |> Seq.choose (fun x -> x.TryGetAttribute("id") |> Option.map (fun a -> a.Value(), x)) 
            |> Seq.filter (fun (x, y) -> x = tableWithPrice)
            |> Seq.map (fun (x, y) -> y.Descendants [tableRows])
            |> Seq.concat
            |> Seq.map (fun x -> (x.Descendants ["th"]), (x.Descendants "td"))
            |> Seq.map (fun (x, y) -> (x |>  Seq.map (fun a -> a.InnerText()), y |> Seq.map (fun a -> a.InnerText())))
            |> Seq.filter (fun (x, y) -> (x |>  Seq.exists (fun a -> a.ToLower().Contains "price")))
            |> Seq.map (fun (x, y) -> y)
            |> Seq.concat
            |> Seq.map (fun x -> x.Trim())
            |> Seq.tryHead

        let image (body: HtmlDocument) = 
            body.Descendants ["img"]
            |> Seq.choose (fun x -> x.TryGetAttribute "src" |> Option.map (fun y -> (y.Value(), x)))
            |> Seq.choose (fun (a, x) -> x.TryGetAttribute "id" |> Option.map (fun y ->(a, y.Value(), x)))
            |> Seq.filter (fun (a, x, y) -> x = "mainImage")
            |> Seq.map (fun (a, x, y) -> a)
            |> Seq.tryHead


        let views (body: HtmlDocument) = 
            body.Descendants ["div"]
            |> Seq.choose (fun x -> x.TryGetAttribute "id" |> Option.map (fun y -> y.Value(), x))
            |> Seq.filter (fun (x, y) -> x = "DetailsFooter_PageViewsPanel")
            |> Seq.map (fun (x, y) -> y)
            |> Seq.map (fun x -> x.Descendants "img")
            |> Seq.concat
            |> Seq.choose (fun x -> x.TryGetAttribute "alt" |> Option.map (fun y -> y.Value()))
            |> Seq.fold (+) ""
            |> (fun x -> match Int32.TryParse x with 
                         | true, a -> Some a
                         | false, _ -> None )

        let link = sprintf "http://www.trademe.co.nz/Browse/Listing.aspx?id=%d" listingId
        
        let downloadListingAysnc () = 
        
            let createStreamReader (encode: Text.Encoding) (data: IO.Stream) = 
                new StreamReader(data, encode)
                
            let readAllData (streamReader: StreamReader) = 
                let data = streamReader.ReadToEnd ()
                streamReader.Dispose ()
                data
                
            let toHtmlDocument data = 
                HtmlDocument.Parse(data)
                
            async {
                try 
                    return! HtmlDocument.AsyncLoad(link)
                with 
                | :? System.Net.WebException as webException -> 
                                                                return webException.Response.GetResponseStream () 
                                                                |> createStreamReader (System.Text.Encoding.GetEncoding("utf-8"))
                                                                |> readAllData 
                                                                |> toHtmlDocument
                                                                
                | e -> logger <| sprintf "Error:%A%s\n%A" (e.GetType ()) e.Message e.StackTrace
                       return HtmlDocument.New Seq.empty
            }
        
        let! body = downloadListingAysnc ()
        return match wasRemoved body, isExpired body with 
                | true, _ -> Some <| {
                                       Listing = { Price = "Removed"; Title = "This listing was withdrawn by the administrator"; ListingId = listingId }
                                       DateAdded = DateTime.Now
                                       Views = 0
                                       Image = None 
                                       WasRemovedOrSold = true }
                | _, true -> Some <| {
                               Listing = { Price = "Sold"; Title = "This listing is no longer available"; ListingId = listingId }
                               DateAdded = DateTime.Now
                               Views = 0
                               Image = None
                               WasRemovedOrSold = true }
                | _, _ -> 
                    header body |> Option.bind (fun header ->  
                    views body |> Option.bind (fun views -> 
                            askingPrice body |> Option.map (fun askingPrice -> 
                            {
                                Listing = {Price = askingPrice; Title = header; ListingId = listingId}
                                DateAdded = DateTime.Now
                                Views = views
                                Image = image body |> Option.map Uri 
                                WasRemovedOrSold = false } )))
    }


let refresh logger (listingItems: FullListing list) =

    let IdsToRemove = listingItems |> List.filter (fun x -> x.Listing.Price = "Sold") |> List.map (fun x -> x.Listing.ListingId)

    let updateListings = 
        listingItems 
            |> List.filter (fun x -> IdsToRemove |> List.exists (fun y -> y = x.Listing.ListingId) |> not )
            |> List.map (fun x -> x.Listing.ListingId)
            |> Set.ofList
            |> Set.toSeq
            |> Seq.map (AsyncQueryListing logger)
            |> Async.Parallel 
            |> Async.RunSynchronously 
            |> Array.toList 
            |> List.choose (fun x -> x)

    let newItems = Set.difference (updateListings |> List.map (fun x -> x.Listing) |> Set.ofList) (listingItems |> List.map (fun x -> x.Listing) |> Set.ofList )
    updateListings |> List.filter (fun x -> newItems |> Set.toList |> List.exists (fun y -> y.ListingId = x.Listing.ListingId))
    
    
let validateListing (logger: string -> unit) input = 

    let parseUrl (url: string) = 
        url.ToCharArray()
        |> Array.filter Char.IsNumber
        |> String

    let asInt = Int32.TryParse >> function
                | true, x -> Some x
                | false,_  -> None
   
    input 
    |> parseUrl 
    |> (fun x -> logger <| sprintf "Id: %s" x; x)
    |> asInt
    |> Option.bind (AsyncQueryListing logger >> Async.RunSynchronously)


let listingIdToLink listingId = 
    sprintf "http://www.trademe.co.nz/Browse/Listing.aspx?id=%d" listingId