module Database
open System
open SQLite
open System.IO

let dbName = "database.db3"

[<AllowNullLiteral>]
type Listing() = 
  [<PrimaryKey>]
  member val Id: int = 0 with get, set

[<AllowNullLiteral>]
type ListingItem() = 
    [<PrimaryKey>]
    member val ListingId: int = 0 with get, set
    member val Price: string = "" with get, set
    member val Title: string = "" with get, set
    member val DateAdded: DateTime = DateTime.MinValue with get, set
    member val Views: int = 0 with get, set
    member val Image: string = null with get, set   
    member val WasRemovedOrSold: bool = false with get, set   

let runMigration path = 
  let connection = new SQLiteConnection(Path.Combine(path, dbName), false)
  connection.CreateTable<Listing>() |> ignore
  connection.CreateTable<ListingItem>() |> ignore
  
  connection