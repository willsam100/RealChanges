module BackgroundTasks 
open SQLite
open ListingDownloader
open System
open System.Diagnostics
open Database
open Gjallarhorn

type ApiClient() = 
    static let mutable dbConnection: SQLiteConnection = null
    
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
        
    member this.GetListingItems(): FullListing list = 
        dbConnection.Table<Database.ListingItem>() 
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

    member this.Set(value: FullListing) = 
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
        dbConnection.Insert(dbRow) |> ignore
        
    member this.DeleteListing(value: FullListing) = 
        let rows = dbConnection.Table<Database.ListingItem>() 
                  |> Seq.filter (fun x -> x.ListingId = listingIdToString value.Listing.ListingId)
                  |> Seq.toList
                  
        rows |> List.iter (fun x -> dbConnection.Delete(x) |> ignore)
         
    member this.Open(path: string, provider: SQLitePCL.ISQLite3Provider) =
      Debug.WriteLine <| sprintf "[API] Opening"
      dbConnection <- runMigration path

    member this.Close() =
      Debug.WriteLine <| sprintf "[API] Closing"
      dbConnection.Close()

    interface System.IDisposable with
        member this.Dispose() =
          Debug.WriteLine <| sprintf "[API] Disposing"
          dbConnection.Dispose()
          
          
let deleteListing (db: ApiClient) (source : ObservableSource<_>) (listingItems: FullListing list) (listingId: ListingId) =
    let cts = new System.Threading.CancellationTokenSource()
    let wf = async {
    
        Debug.WriteLine <| sprintf "Deleting listing: %A items: %A" listingId listingItems
        try 
            listingItems |> List.iter db.DeleteListing
            ReCoreVerTwo.Update.DeletedListing listingId |> source.Trigger
            Debug.WriteLine <| sprintf "Listing deleted from db: %A" listingId
        with 
        | e -> Debug.WriteLine <| sprintf "DB Error: %s" e.Message 
    }
    Async.Start(wf, cancellationToken = cts.Token)
    
let fetchData (db: ApiClient) (source : ObservableSource<_>) () =
    let cts = new System.Threading.CancellationTokenSource()
    let wf = async {
        db.GetListingItems() |> ReCoreVerTwo.FetchItems |> source.Trigger
    }
    Async.Start(wf, cancellationToken = cts.Token)
    
let refreshListings (source : ObservableSource<_>) (listingItems: FullListing list)  =
    let cts = new System.Threading.CancellationTokenSource()
    let wf = async {
        listingItems |> ListingDownloader.refresh Debug.WriteLine |> ReCoreVerTwo.RefrehedItems |> source.Trigger
    }
    Async.Start(wf, cancellationToken = cts.Token)
    
let saveItem: ApiClient -> (string -> unit) -> FullListing -> unit = 
    fun db logger listingItem -> 
    try 
        db.Set(listingItem)
    with 
    | e -> logger <| sprintf "Error saving item: %s\n%s" e.Message e.StackTrace
    
let validateListing (source : ObservableSource<_>) url = 
    let cts = new System.Threading.CancellationTokenSource()
    let wf = async {
        url |> ListingDownloader.validateListing Debug.WriteLine |> ReCoreVerTwo.ItemValidated |> source.Trigger
    }
    Async.Start(wf, cancellationToken = cts.Token)