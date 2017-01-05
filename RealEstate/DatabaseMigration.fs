module DatabaseMigration
open System
open SQLite
open System.IO

let dbName = "database.db3"
let dbVersion = 1

[<AllowNullLiteral>]
type Listing() = 
  [<PrimaryKey>]
  member val Id: int = 0 with get, set

[<AllowNullLiteral>]
type ListingItem() = 
  member val ListingId: int = 0 with get, set
  member val Price: string = "" with get, set
  member val Title: string = "" with get, set
  member val DateAdded: DateTime = DateTime.MinValue with get, set
  member val Views: int = 0 with get, set
  member val Image: string = null with get, set


let migrations = 
  // TODO when required
  // return a list of functions to be exectud for each migration
  [(fun x -> ())]

let runMigration path provider = 

  //SQLitePCL.raw.SetProvider(provider)
  let connection = new SQLiteConnection(Path.Combine(path, dbName), false)
  connection.CreateTable<Listing>() |> ignore
  connection.CreateTable<ListingItem>() |> ignore

  migrations |> List.iter (fun f -> f())
  connection