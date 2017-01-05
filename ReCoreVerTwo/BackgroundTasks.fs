﻿module BackgroundTasks 
open SQLite
open ListingDownloader
open System
open System.Diagnostics
open Database
open Gjallarhorn

type ApiClient() = 
    static let mutable dbConnection: SQLiteConnection = null

    member this.GetListingItems(): FullListing list = 
        dbConnection.Table<Database.ListingItem>() 
        |> Seq.toList 
        |> List.map (fun x -> {
                                Listing = {Price = x.Price; Title = x.Title; ListingId = x.ListingId;}
                                DateAdded = x.DateAdded; 
                                Views = x.Views
                                Image = if (x.Image = null) then None else Some <| Uri x.Image
                                WasRemovedOrSold = x.WasRemovedOrSold
                                }) 

    member this.Set(value: FullListing) = 
        let dbRow = Database.ListingItem()
        dbRow.Price <- value.Listing.Price
        dbRow.Title <- value.Listing.Title
        dbRow.ListingId <- value.Listing.ListingId
        dbRow.DateAdded <- DateTime.Now
        dbRow.Views <- value.Views
        dbRow.Image <- value.Image |> Option.map (fun x -> x.AbsoluteUri) |> Option.toObj
        dbRow.WasRemovedOrSold <- value.WasRemovedOrSold
        dbConnection.Insert(dbRow) |> ignore
        
    member this.DeleteListing(value: FullListing) = 
        let dbRow = Database.ListingItem()
        dbRow.ListingId <- value.Listing.ListingId
        dbConnection.Delete(dbRow) |> ignore
         
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
          
          
let deleteListing (db: ApiClient) (source : ObservableSource<_>) (listingItems: FullListing list) listingId =
    let cts = new System.Threading.CancellationTokenSource()
    let wf = async {
    
        Debug.WriteLine <| sprintf "Deleting listing: %d items: %A" listingId listingItems
        try 
            listingItems |> List.iter db.DeleteListing
            ReCoreVerTwo.Update.DeletedListing listingId |> source.Trigger
            Debug.WriteLine <| sprintf "Listing deleted from db: %d" listingId
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
    
let saveItem: ApiClient -> FullListing -> unit = 
    fun db listingItem -> db.Set(listingItem)
    
let validateListing (source : ObservableSource<_>) url = 
    let cts = new System.Threading.CancellationTokenSource()
    let wf = async {
        url |> ListingDownloader.validateListing Debug.WriteLine |> ReCoreVerTwo.ItemValidated |> source.Trigger
    }
    Async.Start(wf, cancellationToken = cts.Token)