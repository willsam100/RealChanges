module Database
open System
open SQLite
open System.IO

let dbName = "database.db3"

[<AllowNullLiteral>]
type ListingItem() = 
    [<PrimaryKey; AutoIncrement>]
    member val Id: int = 0 with get, set
    member val ListingId: string = "" with get, set
    member val Price: string = "" with get, set
    member val Title: string = "" with get, set
    member val DateAdded: DateTime = DateTime.MinValue with get, set
    member val Views: int = 0 with get, set
    member val Image: string = null with get, set 
    member val IsActive: bool = true with get, set   

let runMigration path = 
  let connection = new SQLiteConnection(Path.Combine(path, dbName), false)
  connection.CreateTable<ListingItem>() |> ignore
  connection