module BackgroundTasks 
open SQLite
open ListingDownloader
open System
open System.Diagnostics
open Database
open Gjallarhorn
open HockeyApp
open System.Collections.Generic

let logException message (properties: Map<string, string>) = 
    let dict = new Dictionary<string, string> ()
    properties |> Map.iter (fun k v-> dict.Add(k,v)) 
    MetricsManager.TrackEvent(message, dict, null)
    Debug.WriteLine <| sprintf "%s %s" message (properties |> Map.fold (fun sum key value -> sprintf "%s %s:%s" sum key value) "")

let dbCatch message f = 
    try 
        f () |> Right
    with 
    | e -> logException message <| Map (["StackTrace", e.StackTrace])
           Left <| sprintf "Error: %A" message

let tradeMe = "TradeMe"
let realEstate = "RealEstate"

let listingIdToString = 
    function
    | TradeMe x -> sprintf "%s:%s" tradeMe x
    | RealEstate x -> sprintf "%s:%s" realEstate x

let stringTolistingId (x: string) = 
    match x.Contains(tradeMe), x.Contains(realEstate) with 
    | true, _ -> Some <| ListingId.TradeMe (x.Replace(tradeMe, "").Replace(":", ""))
    | _, true -> Some <| ListingId.RealEstate (x.Replace(realEstate, "").Replace(":", ""))
    | _, _ -> None
    
let getListingItems (c: SQLiteConnection) = 
    dbCatch "Loading listings" <| fun () -> 
        c.Table<Database.ListingItem>() 
        |> Seq.choose (fun x -> 
            x.ListingId
            |> stringTolistingId
            |> Option.map (fun listingId -> 
                                {
                                    Listing = {Price = x.Price; Title = x.Title; ListingId = listingId}
                                    DateAdded = x.DateAdded; 
                                    Views = x.Views
                                    Image = if (x.Image = null) then None else Some <| Uri x.Image
                                    IsActive = x.IsActive
                                }) )
        |> Seq.toList 
        |> List.map (fun x -> Debug.WriteLine <| sprintf "Loaded listing: %A" x ; x)

let saveItem (c: SQLiteConnection) (value: FullListing) = 
    dbCatch "Loading listings" <| fun () -> 
        let dbRow = Database.ListingItem()
        
        let (id, source) = match value.Listing.ListingId with 
                            | TradeMe x -> (x, tradeMe)
                            | RealEstate x -> (x, realEstate)
        
        dbRow.Price <- value.Listing.Price
        dbRow.Title <- value.Listing.Title
        dbRow.ListingId <- listingIdToString value.Listing.ListingId
        dbRow.DateAdded <- DateTime.Now
        dbRow.Views <- value.Views
        dbRow.Image <- value.Image |> Option.map (fun x -> x.AbsoluteUri) |> Option.toObj
        dbRow.IsActive <- value.IsActive
        c.Insert(dbRow) |> ignore
    
let deleteListingRecord (c: SQLiteConnection) (value: FullListing) = 
    dbCatch "Loading listings" <| fun () -> 
        let rows = c.Table<Database.ListingItem>() 
                  |> Seq.filter (fun x -> x.ListingId = listingIdToString value.Listing.ListingId)
                  |> Seq.toList
                  
        rows |> List.iter (fun x -> c.Delete(x) |> ignore)
     
let openConnection (path: string) =
  runMigration path

let private foldErrors result = 
    function
    | Right x -> result
    | Left x -> Some x

let deleteListing (c: SQLiteConnection) (source : ObservableSource<_>) (listingItemsWithListingId: FullListing list) (listingId: ListingId) =
    let cts = new System.Threading.CancellationTokenSource()
    let wf = async {

            listingItemsWithListingId 
            |> List.map (deleteListingRecord c)
            |> List.fold foldErrors None
            |> function
            | None -> RealEstateCore.Update.DeletedListing listingId |> source.Trigger
            | Some errorMessage -> RealEstateCore.Update.DeletedListingFailed errorMessage |> source.Trigger
    }
    Async.Start(wf, cancellationToken = cts.Token)
    
let fetchData (c: SQLiteConnection) (source : ObservableSource<_>) () =
    let cts = new System.Threading.CancellationTokenSource()
    let wf = async {
            getListingItems c 
            |> function
                | Right x -> x |> RealEstateCore.FetchItems |> source.Trigger
                | Left e -> [] |> RealEstateCore.FetchItems |> source.Trigger
    }
    Async.Start(wf, cancellationToken = cts.Token)

let refreshListings logger (c: SQLiteConnection) (source : ObservableSource<_>) (listingItems: FullListing list)  =
    let cts = new System.Threading.CancellationTokenSource()
    let wf = async {
            let newItems = listingItems |> ListingDownloader.refresh logger 
            newItems |> List.map (saveItem c) |> List.fold foldErrors None 
            |> function
                | None -> newItems |> RealEstateCore.RefrehedItems |> source.Trigger
                | Some e -> [] |> RealEstateCore.RefrehedItems |> source.Trigger
    }

    Async.Start(wf, cancellationToken = cts.Token)

let validateListing logException (source : ObservableSource<_>) url = 
    let cts = new System.Threading.CancellationTokenSource()
    let wf = async {
            url |> ListingDownloader.validateListing logException |> RealEstateCore.ItemValidated |> source.Trigger
    }
    Async.Start(wf, cancellationToken = cts.Token)