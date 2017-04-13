module ListingDownloader
open FSharp.Data
open System
open System.IO
open System.Net

type Either<'a,'b> = Choice<'b,'a>
let  Right x :Either<'a,'b> = Choice1Of2 x
let  Left  x :Either<'a,'b> = Choice2Of2 x
let  (|Right|Left|) = function Choice1Of2 x -> Right x | Choice2Of2 x -> Left x

type ListingId = 
    | RealEstate of string
    | TradeMe of string

type SimpleListing = {
    ListingId: ListingId
    Price: string
    Title: string
}

type FullListing = {
    Listing: SimpleListing
    DateAdded: DateTime
    Views: int
    Image: Uri option
    IsActive: bool
}

module RealEstate = 

    let isExpired (body: HtmlDocument) = 
        body.Descendants ["h1"]
        |> Seq.filter (fun x -> x.InnerText () = "This listing is no longer on the realestate.co.nz website")
        |> Seq.length 
        |> (fun count -> count = 1)
        
    let wasRemoved (body: HtmlDocument) = 
        false
    
    let header (body: HtmlDocument) = 
        body.Descendants ["h1"] 
        |> Seq.choose (fun x -> x.TryGetAttribute("itemprop") |> Option.map (fun a -> a.Value(), x)) 
        |> Seq.filter (fun (x, y) -> x = "name")
        |> Seq.map (fun (x,y) -> y.InnerText().Trim())
        |> Seq.tryHead
        
    let askingPrice (body: HtmlDocument) = 
        body.Descendants ["h2"] 
        |> Seq.map (fun (x) -> x.InnerText().Trim())
        |> Seq.tryHead

    let image (body: HtmlDocument) = 
        body.Descendants ["div"]
        |> Seq.choose (fun x -> x.TryGetAttribute("id") |> Option.map (fun a -> a.Value(), x)) 
        |> Seq.filter (fun (x, y) -> x = "mainImageHolder")
        |> Seq.map (fun (x,y) -> y.Descendants ["img"])
        |> Seq.concat
        |> Seq.choose (fun x -> x.TryGetAttribute "src" |> Option.map (fun y -> y.Value ()))
        |> Seq.tryHead
        
        
    let views (body: HtmlDocument) = 
        body.Descendants ["span"]
        |> Seq.choose (fun x -> x.TryGetAttribute("class") |> Option.map (fun a -> a.Value(), x)) 
        |> Seq.filter (fun (x, y) -> x = "stats")
        |> Seq.map (fun (x,y) -> y.Descendants ["a"])
        |> Seq.concat
        |> Seq.choose (fun x -> x.TryGetAttribute "href" |> Option.map (fun a -> x))
        |> Seq.map (fun x -> x.InnerText())
        |> Seq.filter (fun x -> x.Contains "Listing Views")
        |> Seq.tryHead
        |> Option.map (fun x -> x.Replace("Listing Views", "").Trim())
        |> Option.map (fun x -> x.Replace(",", ""))
        |> Option.map (Int32.TryParse)
        |> Option.bind (fun (x,y) -> if x then Some y else None)
        
    let listingId (body: HtmlDocument) = 
        body.Descendants ["h4"]
        |> Seq.map (fun x -> x.Descendants ["b"])
        |> Seq.concat
        |> Seq.map (fun x -> x.InnerText())
        |> Seq.filter (fun x -> x.Contains "Listing")
        |> Seq.tryHead
        |> Option.map (fun x -> x.Replace("Listing", "").Replace("#", "").Trim())
        
        
module TradeMe = 
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

let handleException logger link action = 

    let createStreamReader (encode: Text.Encoding) (data: IO.Stream) = new StreamReader(data, encode)
    let toHtmlDocument data = HtmlDocument.Parse(data)
    let readAllData (streamReader: StreamReader) = 
        try 
            let data = streamReader.ReadToEnd ()
            streamReader.Dispose ()
            data
        with 
        | e -> "<p>empty</p>"

    let safeReadData (x: Net.WebResponse) = 
        try 
            x.GetResponseStream () 
            |> createStreamReader (System.Text.Encoding.GetEncoding("utf-8"))
            |> readAllData 
            |> toHtmlDocument
        with 
        | e -> HtmlDocument.New Seq.empty

    let filterHandledStatusCodes x = 
        x = HttpStatusCode.NotFound


    let evaluateException: exn -> (string * string) list = 
        fun ex -> [("exception", ex.StackTrace); ("message", ex.Message)]

    try 
        action ()
    with 
    | :? WebException as webException -> 
            
            let webExceptionOption = Option.ofObj webException 
            let data = webExceptionOption |> Option.map evaluateException |> defaultArg <| []

            webExceptionOption 
            |> Option.bind (fun x -> x.Response |> Option.ofObj) 
            |> Option.bind (fun x -> x :? HttpWebResponse 
                                     |> function | true -> Some (x :?> HttpWebResponse) | false -> None )
            |> Option.map (fun x -> x.StatusCode)
            |> Option.filter filterHandledStatusCodes
            |> function | Some x -> None | None -> Some true
            |> Option.iter (fun x -> logger "webException" <| Map (["listingID", link] @ data))

            webExceptionOption
            |> Option.bind (fun x ->  Option.ofObj x.Response)
            |> Option.map safeReadData
            |> function 
                | Some x -> x 
                | None -> logger "WebExcceptionFailedParse" <| Map (["listingID", link] @ data)
                          HtmlDocument.New Seq.empty
                                    
    | e -> 
            let webExceptionOption = Option.ofObj e 
            logger "Exception" <| Map (["listingID", link] @ (evaluateException e))

            HtmlDocument.New Seq.empty

let asyncQueryListing (logger: string -> Map<string, string> -> unit) listingIdWithSource = 

    let asyncQueryListing link wasRemoved isExpired header askingPrice image views listingId =  
        async {
            let body = handleException logger link <| fun () -> HtmlDocument.AsyncLoad(link) |> Async.RunSynchronously
            return match wasRemoved body, isExpired body with 
                    | true, _ -> Some <| {
                                           Listing = { Price = "Removed"; Title = "This listing was withdrawn by the administrator"; ListingId = listingId }
                                           DateAdded = DateTime.Now
                                           Views = 0
                                           Image = None 
                                           IsActive = false }
                    | _, true -> Some <| {
                                   Listing = { Price = "Sold or Removed"; Title = "This listing is no longer available"; ListingId = listingId }
                                   DateAdded = DateTime.Now
                                   Views = 0
                                   Image = None
                                   IsActive = false }
                    | _, _ -> 
                        let result = 
                            header body |> Option.bind (fun header ->  
                            views body |> Option.bind (fun views -> 
                                askingPrice body |> Option.map (fun askingPrice -> 
                                {
                                    Listing = {Price = askingPrice; Title = header; ListingId = listingId}
                                    DateAdded = DateTime.Now
                                    Views = views
                                    Image = image body |> Option.map Uri 
                                    IsActive = true } )))
                        match result with 
                        | Some x -> result
                        | None -> logger "ParseFailure" <| Map [("link", link)]
                                  None
    
        }
    
    match listingIdWithSource with  
    | TradeMe listingId -> 
        let link = sprintf "http://www.trademe.co.nz/Browse/Listing.aspx?id=%s" listingId
        asyncQueryListing link TradeMe.wasRemoved TradeMe.isExpired TradeMe.header TradeMe.askingPrice TradeMe.image TradeMe.views listingIdWithSource
    | RealEstate listingId -> 
        let link = sprintf "http://www.realestate.co.nz/%s" listingId
        asyncQueryListing link RealEstate.wasRemoved RealEstate.isExpired RealEstate.header RealEstate.askingPrice RealEstate.image RealEstate.views listingIdWithSource


let refresh logger (listingItems: FullListing list) =
    async {
        
        let IdsToRemove = listingItems |> List.filter (fun x -> x.Listing.Price = "Sold" || x.IsActive = false ) |> List.map (fun x -> x.Listing.ListingId)
        let updateListings = 
            listingItems 
                |> List.filter (fun x -> IdsToRemove |> List.exists (fun y -> y = x.Listing.ListingId) |> not )
                |> List.map (fun x -> x.Listing.ListingId)
                |> Set.ofList
                |> Set.toSeq
                |> Seq.map (asyncQueryListing logger)
                |> Async.Parallel 
                |> Async.RunSynchronously
                |> Array.toList 
                |> List.choose (fun x -> x)

        let newItems = Set.difference (updateListings |> List.map (fun x -> x.Listing) |> Set.ofList) (listingItems |> List.map (fun x -> x.Listing) |> Set.ofList )
        return updateListings |> List.filter (fun x -> newItems |> Set.toList |> List.exists (fun y -> y.ListingId = x.Listing.ListingId))
    } |> Async.RunSynchronously
    
    
let validateListing (logger: string -> Map<string, string> -> unit) input = 

    let parseUrl (url: string) = 
    
        match url.ToLower().Contains("trademe"), url.ToLower().Contains("realestate.co.nz") with 
        | true, _ -> 
                    url.ToCharArray()
                    |> Array.filter Char.IsNumber
                    |> String
                    |> TradeMe
                    |> Some
        | _, true -> 
                    url.ToCharArray()
                    |> Array.filter Char.IsNumber
                    |> String
                    |> RealEstate
                    |> Some
        | _, _ -> None
   
    input 
    |> parseUrl 
    |> Option.bind (asyncQueryListing logger >> Async.RunSynchronously)

let listingIdToLink = 
    function
    | TradeMe x -> sprintf "http://www.trademe.co.nz/Browse/Listing.aspx?id=%s" x
    | RealEstate x -> sprintf "http://realestate.co.nz/%s" x
    
let listingIdToString = 
    function
    | TradeMe x -> sprintf "TradeMe # %s" x
    | RealEstate x -> sprintf "RealEstate # %s" x
    
    
