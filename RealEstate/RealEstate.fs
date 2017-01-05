namespace Re.Core

open Xamarin.Forms
open Xamarin.Forms.Xaml
open FSharp.Data
open DatabaseMigration
open SQLite
open System.Diagnostics
open System
open System.Threading.Tasks

module RealEstateVersionOne = 

    type SnapshotListing = {
        ListingId: int
        Price: string
        Title: string
    }

    type ListingItem = {
      ListingId: int
      Price: string
      Title: string
      DateAdded: DateTime
      Views: int
      Image: Uri option
    }

    type ApplicationState = {
        ListingItems: ListingItem list
    }

     
    type Listing = {
      Id: int
    }

    type ProgressDialog = {
        Show: unit -> unit 
        Hide: unit -> unit
        Toast: string -> unit
    }

    type Message =
        | Add of (string * AsyncReplyChannel<bool>)
        | Refresh
        | Load
        | GetData of AsyncReplyChannel<ListingItem list>
        | RefreshedData of ListingItem list


    type ApiClient() = 
        static let mutable dbConnection: SQLiteConnection = null

        member this.GetListings() = 
            dbConnection.Table<DatabaseMigration.Listing>() |> Seq.toList |> List.map (fun x -> {Id = x.Id}) 

        member this.GetListingItems() = 
            dbConnection.Table<DatabaseMigration.ListingItem>() 
            |> Seq.toList 
            |> List.map (fun x -> {
                                    Price = x.Price; 
                                    Title = x.Title; 
                                    ListingId = x.ListingId;
                                    DateAdded = x.DateAdded; 
                                    Views = x.Views
                                    Image = if (x.Image = null) then None else Some <| Uri x.Image
                                    }) 

        member this.Set(value:ListingItem) = 
            let dbRow = DatabaseMigration.ListingItem()
            dbRow.Price <- value.Price
            dbRow.Title <- value.Title
            dbRow.ListingId <- value.ListingId
            dbRow.DateAdded <- DateTime.Now
            dbRow.Views <- value.Views
            dbRow.Image <- value.Image |> Option.map (fun x -> x.AbsoluteUri) |> Option.toObj
            dbConnection.Insert(dbRow) |> ignore

        member this.Set(value:Listing) = 
            let dbRow = DatabaseMigration.Listing()
            dbRow.Id <- value.Id
            dbConnection.InsertOrReplace(dbRow) |> ignore
             
        member this.Open(path: string, provider: SQLitePCL.ISQLite3Provider) =
          Debug.WriteLine <| sprintf "[API] Opening"
          dbConnection <- runMigration path provider

        member this.Close() =
          Debug.WriteLine <| sprintf "[API] Closing"
          dbConnection.Close()

        interface System.IDisposable with
            member this.Dispose() =
              Debug.WriteLine <| sprintf "[API] Disposing"
              dbConnection.Dispose()


    let safeHead (x: 'a list) = 
        match x.Length with 
                      | 1 -> Some (x.Head)
                      | _ -> None

    let AsyncQueryListing listingId = 
        async {
            let tableWithPrice = "ListingAttributes"
            let tableRows = "tr"

            let isExpired (body: HtmlDocument) = 
                body.Descendants ["h1"]
                |> Seq.map (fun (x) -> x.InnerText())
                |> Seq.map (fun x -> x = "Sorry, this classified has expired.")
                |> Seq.fold (||) false

            let header (body: HtmlDocument) = 
                body.Descendants ["h1"] 
                |> Seq.choose (fun x -> 
                   x.TryGetAttribute("id") |> Option.map (fun a -> x.InnerText())) 
                |> Seq.map (fun x -> x.Trim()) 
                |> Seq.toList
                |> safeHead

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
                |> Seq.toList
                |> safeHead

            let image (body: HtmlDocument) = 
                body.Descendants ["img"]
                |> Seq.choose (fun x -> x.TryGetAttribute "src" |> Option.map (fun y -> (y.Value(), x)))
                |> Seq.choose (fun (a, x) -> x.TryGetAttribute "id" |> Option.map (fun y ->(a, y.Value(), x)))
                |> Seq.filter (fun (a, x, y) -> x = "mainImage")
                |> Seq.map (fun (a, x, y) -> a)
                |> Seq.toList
                |> safeHead


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
            let! body = HtmlDocument.AsyncLoad(link)
            return match isExpired body with 
                   | true -> Some <| {
                                   Price = "Sold"
                                   Title = "This listing is no longer available"
                                   ListingId = listingId
                                   DateAdded = DateTime.Now
                                   Views = 0
                                   Image = None }
                    | false -> 
                        header body |> Option.bind (fun header ->  
                        views body |> Option.bind (fun views -> 
                                askingPrice body |> Option.map (fun askingPrice -> 
                                {
                                    Price = askingPrice
                                    Title = header
                                    ListingId = listingId
                                    DateAdded = DateTime.Now
                                    Views = views
                                    Image = image body |> Option.map Uri} )))
        }

    let refresh listings listingItems (reply: ListingItem list -> unit) =

        let asSnapshots s: SnapshotListing List = s |> List.map (fun x -> {Title = x.Title; ListingId = x.ListingId; Price = x.Price})

        let IdsToRemove = listingItems |> List.filter (fun x -> x.Price = "Sold") |> List.map (fun x -> x.ListingId)

        let updateListings = 
            listings 
                |> List.filter (fun x -> IdsToRemove |> List.contains x.Id |> not )
                |> List.map (fun x -> x.Id)
                |> List.toSeq 
                |> Seq.map(fun x -> AsyncQueryListing x) 
                |> Async.Parallel 
                |> Async.RunSynchronously 
                |> Array.toList 
                |> List.choose (fun x -> x)

        let newItems = Set.difference (asSnapshots updateListings |> Set.ofList) (listingItems |> asSnapshots |> Set.ofList )
        updateListings |> List.filter (fun x -> newItems |> Set.toList |> List.exists (fun y -> y.ListingId = x.ListingId)) |> reply
       
    let validateListing id = 
        let asInt = match Int32.TryParse id with 
                    | true, x -> Some x
                    | false,_  -> None
       
        asInt 
        |> Option.bind (fun x -> x |> AsyncQueryListing |> Async.RunSynchronously)
        |> Option.map (fun x -> ({Id = x.ListingId}, x))


    let runTask task = 
        do 
          async {
            do! task
                |> Async.AwaitIAsyncResult 
                |> Async.Ignore
            return ()
          } |> Async.StartImmediate

    let runOnUi f = 
        Action f |> Xamarin.Forms.Device.BeginInvokeOnMainThread 

    let setBinding (name: string) property (x: View) = 
        x.SetBinding(property, name)
        x

    type Store(db: ApiClient) = 
        let event = Event<ApplicationState> ()
        let updated = event.Publish

        let loadItemsIfRequired x y = 
            try 
                match x, y with 
                | [], [] -> let items = db.GetListings()
                            let listingItems = db.GetListingItems()
                            (items, listingItems)
                | x, y -> (x, y)
            with 
            | ex -> Debug.WriteLine <| sprintf "Failed to load items %s" ex.Message
                    ([], [])

        let parseUrl (url: string) = 
            url.Replace("http://www.trademe.co.nz/Browse/Listing.aspx?id=", "")

        
        let mbox = MailboxProcessor.Start(fun mbox ->
            let rec loop(items: Listing list, listingItems: ListingItem list) = async { 
                let! msg = mbox.Receive()

                Debug.WriteLine("Received message");
                match msg with 
                    | Add(url, chnl) ->
                        let result = url |> parseUrl |> validateListing
                        match result with 
                        | Some (listing, listingItem) -> 
                                                        db.Set listing
                                                        db.Set listingItem
                                                        chnl.Reply true
                                                        let items = listing :: items
                                                        let listingItems = listingItem :: listingItems
                                                        event.Trigger {ListingItems = listingItems}
                                                        return! loop (items, listingItems)
                        | None -> chnl.Reply true
                                  return! loop (items, listingItems)

                    | Load ->
                            let (items, listingItems) = loadItemsIfRequired items listingItems
                            event.Trigger {ListingItems = listingItems}
                            return! loop (items, listingItems)

                    | Refresh -> 
                        let (items, listingItems) = loadItemsIfRequired items listingItems
                        refresh items listingItems (fun items -> mbox.Post <| RefreshedData items)
                        return! loop (items, listingItems)

                    | RefreshedData udpatedItems -> 

                        let listingItems = udpatedItems @ listingItems
                        event.Trigger {ListingItems = listingItems}
                        udpatedItems |> List.iter db.Set
                        return! loop (items, listingItems)

                    | GetData chan -> 
                        chan.Reply listingItems
                        return! loop (items, listingItems)
            }
            loop([], []) )
        
        do 
            Debug.WriteLine("Creating store")

        member this.Updated = updated
        member this.Action = mbox



    type AddListing(store: Store, progressDialog: ProgressDialog) = 
        inherit ContentPage() 

        let label = Label(Text = "Listing link", BackgroundColor = Color.White, TextColor = Color.Black)
        let entry = Entry(TextColor = Color.Black)
        let button = Button(Text = "Track", BackgroundColor = Color.White, TextColor = Color.Black)
        let output = Label(BackgroundColor = Color.White, TextColor = Color.Black)
        let stack = StackLayout(HorizontalOptions = LayoutOptions.FillAndExpand, 
                                VerticalOptions = LayoutOptions.FillAndExpand, 
                                BackgroundColor = Color.White)

        do 
            button.Clicked |> Observable.add (fun x -> 
                progressDialog.Show()
                let result = store.Action.PostAndAsyncReply (fun replyChanel -> Add (entry.Text, replyChanel)) |> Async.RunSynchronously
                progressDialog.Hide()
                match result with 
                | true -> 
                    let id = entry.Text
                    entry.Text <- ""
                    output.Text <- sprintf "Added listing: %s" id
                | false -> 
                    output.Text <- "Failed to add listing"
            )

            let d = entry.PropertyChanged 
                    |> Observable.subscribe (fun x -> output.Text <- "")

            stack.Children.Add(label)
            stack.Children.Add(entry)
            stack.Children.Add(button)
            stack.Children.Add(output)
            base.Content <- stack
            entry.Focus () |> ignore
           
    type HistoryCell() = 
        inherit ViewCell()

        let addChild (layout: StackLayout) item = 
            layout.Children.Add item
        
        let h1 = StackLayout(Orientation = StackOrientation.Horizontal, HorizontalOptions = LayoutOptions.FillAndExpand, BackgroundColor = Color.White)
        let s1 = StackLayout(VerticalOptions = LayoutOptions.FillAndExpand, HorizontalOptions = LayoutOptions.FillAndExpand, BackgroundColor = Color.White)
        let stack = StackLayout(Orientation = StackOrientation.Horizontal, HorizontalOptions = LayoutOptions.FillAndExpand, 
                                VerticalOptions = LayoutOptions.Start, BackgroundColor = Color.White)
                   
        let views = Label(FontSize = 10., HorizontalTextAlignment = TextAlignment.Start, 
                          HorizontalOptions = LayoutOptions.Start, TextColor = Color.Black, BackgroundColor = Color.White)
                    |> setBinding "Views" Label.TextProperty

        let created = Label(FontSize = 10., HorizontalTextAlignment = TextAlignment.End, 
                            HorizontalOptions = LayoutOptions.End, TextColor = Color.Black, BackgroundColor = Color.White)
                    |> setBinding "Created" Label.TextProperty      

        let title = Label(FontSize = 15., HorizontalOptions = LayoutOptions.Start, TextColor = Color.Black, BackgroundColor = Color.White) 
                    |> setBinding "Title" Label.TextProperty

        let price = Label(FontSize = 14., HorizontalOptions = LayoutOptions.Start, TextColor = Color.Black, BackgroundColor = Color.White) 
                    |> setBinding "Price" Label.TextProperty

        do 
            [views; created] |> List.iter (addChild h1)
            [title; price] |> List.iter (addChild s1)
            addChild s1 h1
            addChild stack s1

            base.View <- stack

    type CurrentListingItem() = 
        inherit ViewCell()

        let setBinding (name: string) property (x: View)  = 
            x.SetBinding(property, name)
            x

        let addChild (layout: StackLayout) item = 
            layout.Children.Add item
        
        let h1 = StackLayout(Orientation = StackOrientation.Horizontal, HorizontalOptions = LayoutOptions.StartAndExpand, BackgroundColor = Color.White)
        let s1 = StackLayout(VerticalOptions = LayoutOptions.FillAndExpand, HorizontalOptions = LayoutOptions.StartAndExpand, BackgroundColor = Color.White)
        let stack = StackLayout(Orientation = StackOrientation.Horizontal, 
                                HorizontalOptions = LayoutOptions.FillAndExpand, 
                                VerticalOptions = LayoutOptions.Start, 
                                BackgroundColor = Color.White,
                                MinimumHeightRequest = 100.,
                                HeightRequest = 100.)
       

        let image = Image(HorizontalOptions = LayoutOptions.Start, 
                          VerticalOptions = LayoutOptions.Start,  
                          WidthRequest = 120., HeightRequest = 90., 
                          MinimumHeightRequest = 90., 
                          Aspect = Aspect.Fill, 
                          BackgroundColor = Color.Black, 
                          Margin = Thickness(5.,0.,1.,1.))
                    |> setBinding "Image" Image.SourceProperty
                   
        let views = Label(FontSize = 10., HorizontalTextAlignment = TextAlignment.Start, HorizontalOptions = LayoutOptions.Start, TextColor = Color.Black, BackgroundColor = Color.White)
                    |> setBinding "Views" Label.TextProperty
        let changed = Label(FontSize = 20., HorizontalTextAlignment = TextAlignment.End, HorizontalOptions = LayoutOptions.End, TextColor = Color.Black, BackgroundColor = Color.White)
                    |> setBinding "Changed" Label.TextProperty      

        let title = Label(FontSize = 15., HorizontalOptions = LayoutOptions.Start, TextColor = Color.Black, BackgroundColor = Color.White) 
                    |> setBinding "Title" Label.TextProperty
        let price = Label(FontSize = 14., HorizontalOptions = LayoutOptions.Start, TextColor = Color.Black, BackgroundColor = Color.White) 
                    |> setBinding "Price" Label.TextProperty


        do 
            [views; changed] |> List.iter (addChild h1)
            [title; price] |> List.iter (addChild s1)
            addChild s1 h1

            addChild stack image
            addChild stack s1

            base.View <- stack

        

    type Item(t: string, price:string, views: int, image: Uri, listingId: int, created: DateTime, changed: bool) = 

        member this.Title = t
        member this.Price = price
        member this.Image = image.AbsoluteUri
        member this.ListingId = listingId
        member this.Views = views |> string |> sprintf "Views: %s"
        member this.Created = sprintf "Updated: %s" <| created.ToString("D")
        member this.Changed = match changed with 
                                | true -> "*"
                                | false -> ""

    type ChangesForListing(store: Store, listingId: int) = 
        inherit ContentPage()

        let listView = ListView(ListViewCachingStrategy.RecycleElement, VerticalOptions = LayoutOptions.Fill, 
                                ItemTemplate = DataTemplate(typeof<HistoryCell>),
                                HasUnevenRows = true, BackgroundColor = Color.White)  

        let loadItems (xs: ListingItem list) = 
            xs 
            |> List.toSeq 
            |> Seq.filter (fun x -> x.ListingId = listingId)
            |> Seq.sortByDescending (fun x -> x.DateAdded)


        let items = store.Action.PostAndReply(fun chan -> GetData(chan)) |> loadItems

        let imageHeader = 
            items 
            |> Seq.toList 
            |> Seq.fold (fun image x -> match image, x.Image with 
                                          | Some a, Some b -> Some b
                                          | Some a, None ->   Some a
                                          | None, Some a ->   Some a
                                          | None, None ->     None ) None
            |> Option.map ImageSource.FromUri

        do 
            imageHeader 
            |> Option.iter (fun x -> listView.Header <- Image(Source = x, 
                                                              HorizontalOptions = LayoutOptions.FillAndExpand, 
                                                              HeightRequest = 200., 
                                                              WidthRequest = 420., 
                                                              Aspect = Aspect.AspectFill))

            listView.ItemSelected |> Observable.add (fun x -> listView.SelectedItem <- null)
            listView.ItemsSource <- items |> Seq.map (fun x -> Item(x.Title, x.Price, x.Views, null, x.ListingId, x.DateAdded, false))
            base.Content <- listView
            base.Title <- sprintf "Listing: %d" listingId



    //type ListViewCommand() 

    let navigateToPage page navigationType = 
        page |> navigationType |> runTask


    type CurrentPrices(progressDialog: ProgressDialog, store: Store) as this = 
        inherit ContentPage() 

        let add = ToolbarItem(Text = "Add")

        let loadItems items = 
            items
            |> List.toSeq
            |> Seq.groupBy (fun x -> x.ListingId)
            |> Seq.map (fun (x, xs) -> xs |> Seq.sortBy (fun y -> y.DateAdded) |> Seq.last)
            |> Seq.filter (fun x -> x.Price <> "Sold")
            |> Seq.sortByDescending (fun x -> x.DateAdded)
            |> Seq.map (fun x -> Item(x.Title, x.Price, x.Views, x.Image |> Option.toObj, x.ListingId, x.DateAdded, x.DateAdded > (DateTime.Now.AddHours (-6.))))

        let listView = ListView(ListViewCachingStrategy.RecycleElement, VerticalOptions = LayoutOptions.Fill,
                                ItemTemplate = DataTemplate(typeof<CurrentListingItem>),
                                HasUnevenRows = true,
                                BackgroundColor = Color.White )  


        let command f = Command (Action f)
        do 
            listView.ItemTapped
            |> Observable.map (fun x -> x.Item :?> Item)
            |> Observable.map (fun x -> navigateToPage (ChangesForListing(store, x.ListingId)) this.Navigation.PushAsync)
            |> Observable.add (fun x -> listView.SelectedItem <- null) 

            store.Updated |> Observable.add (fun data -> runOnUi (fun () -> 
                                                                            //let newItems = loadItems data :?> Collections.IEnumerable
                                                                            //if listView.ItemsSource <> newItems then 
                                                                                //progressDialog.Toast "Items updated"

                                                                            //let binding = new Binding("ListingItems")
                                                                            //binding.Source <- {data with ListingItems = loadItems data.ListingItems}
                                                                            listView.ItemsSource <- loadItems data.ListingItems
                                                                            listView.IsPullToRefreshEnabled <- true
                                                                            listView.IsRefreshing <- false
                                                                            //else 
                                                                            //    progressDialog.Toast "No updates :(" 
                                                                                ))

            add.Clicked |> Observable.add (fun _ -> AddListing(store, progressDialog) |> this.Navigation.PushAsync  |> runTask)
            this.Content <- listView
            listView.RefreshCommand <- command (fun () -> Debug.WriteLine "Refreshing list";  store.Action.Post Refresh)
            this.Title <- "Listings"
            this.ToolbarItems.Add add

        override this.OnAppearing() = 
            Debug.WriteLine "Refreshing"
            store.Action.Post Refresh



    type SimpleProgres() =
        member val Show: Action = null with get, set
        member val Hide: Action = null with get, set
        member val Toast: Action<string> = null with get, set


    type App(path: string, provider: SQLitePCL.ISQLite3Provider, simpleDialog: SimpleProgres) =
        inherit Application()

        let db = new ApiClient()
        let store = Store(db)

        do
            db.Open(path, provider)

        let progressDialog = 
            {
                Show = (fun () -> simpleDialog.Show.Invoke()); 
                Hide = (fun () -> (simpleDialog.Hide.Invoke())) 
                Toast = (fun message -> ()) 
            }

        do
            //simpleDialog.Show
            db.Open(path, provider)
            base.MainPage <- NavigationPage(CurrentPrices(progressDialog, store))

            store.Action.Post Load
            store.Action.Post Refresh
