namespace ReCoreVerTwo
open System
open System.Diagnostics
open System.Threading.Tasks
open Xamarin.Forms
open ListingDownloader
open Gjallarhorn
open Gjallarhorn.Bindable
open Gjallarhorn.XamarinForms

type AddListingModel = {
    OutputMessage: string
    ItemAdded: bool
    IsValidatingItem: bool
    EntryText: string
}

type CurrentPage = 
    | ListingsPage
    | AddListingPage
    | ListingChangesPage
    | About

type Model = { 
    Items : FullListing list 
    ShowRemovedListings: bool
    IsRefreshing: bool
    AddListingModel: AddListingModel option
    ListingChanges: int option
    CurrentPage: CurrentPage
}

type Update =
    | FetchItems of FullListing list
    | ItemSaved
    | RefrehedItems of FullListing list
    | ItemValidated of FullListing option
    | DeletedListing of int
    
type NavigationDetails = {
    Page: Page
    Navigation: INavigation -> Page -> Task
    Binding: Component<Model,Msg>
}

and RequestAction = 
    | RequstLoad
    | RequstRefresh
    | AddListingMessage of string
    | SetListingDetail of (NavigationDetails * int)
    | DeleteListing of int
    | ToggleShowRemoved 

and ChangePage = 
    | AddListing of NavigationDetails
    | ListingDetail of NavigationDetails
    | About of NavigationDetails
    | Root

and Msg = 
    | RequestAction of RequestAction
    | RequestCompleted of Update
    | ChangePage of ChangePage
    | Loop
    
module Option = 

    let optionOr left right = 
        match (left, right) with 
            | Some a, Some b -> Some a
            | Some a, None ->   Some a
            | None, Some a ->   Some a
            | None, None ->     None 
            
            

type StateManagement (navPage: INavigation, loadItems: unit -> unit, saveListing, validateListing, refreshListings, deleteListing) as this =

    let listingValidated item (current: Model) = 
        Debug.WriteLine <| sprintf "Listing has been validated"
        match item, current.AddListingModel with 
        | Some listingItem, Some listingModel -> saveListing listingItem
                                                 { current with Items = listingItem :: current.Items 
                                                                AddListingModel = Some { listingModel with 
                                                                                                        OutputMessage = "Listing saved"; 
                                                                                                        ItemAdded = true; 
                                                                                                        IsValidatingItem = false }}
        | Some listingItem, None -> saveListing listingItem
                                    { current with Items = listingItem :: current.Items 
                                                   AddListingModel = Some { EntryText = ""
                                                                            OutputMessage = "Listing saved"; 
                                                                            ItemAdded = true; 
                                                                            IsValidatingItem = false }}
        | None, _ -> {current with AddListingModel = Some {OutputMessage = "Failed to find listing"; ItemAdded = false; IsValidatingItem = false; EntryText = ""}}

    let handleUpdateItems msg current = 
        match msg with 
        | FetchItems xs -> 

            let items = xs |> List.map (fun x -> x.Listing) |> Set.ofList |> Set.toList
            let filteredItems = 
                items 
                |> List.map (fun x -> xs |> List.find (fun y -> y.Listing = x))
                |> List.toSeq
                |> Seq.groupBy (fun x -> x.Listing.ListingId)
                |> Seq.map (fun (key, xs) -> let image = xs |> Seq.fold (fun image x -> Option.optionOr image x.Image ) None
                                             xs |> Seq.map (fun x -> {x with Image = image} ) )
                |> Seq.concat 
                |> Seq.toList
                
            {current with Items = filteredItems }
                           
        | RefrehedItems xs -> xs |> List.iter saveListing
                              {current with Items = xs @ current.Items; IsRefreshing = false}
        | ItemSaved -> {current with AddListingModel = None}
        | ItemValidated x -> listingValidated x current
        | DeletedListing listingId -> 
            Debug.WriteLine <| sprintf "Listing deleted: %d" listingId
            {current with Items = current.Items |> List.filter (fun x -> x.Listing.ListingId <> listingId) }
            

    let processPageChange navigationDetails = 
        let app = Framework.application this.ToSignal (fun () -> ()) this.Update navigationDetails.Binding
        Framework.changePage (navigationDetails.Navigation navPage) app navigationDetails.Page
    
    let handleChangePage cp current = 
        match cp with 
        | ChangePage.AddListing navigationDetails -> 
            Debug.WriteLine <| sprintf "Changing page to AddListing"
            let current = {current with CurrentPage = AddListingPage}
            processPageChange navigationDetails
            current
                                                    
        | ChangePage.ListingDetail navigationDetails -> 
            let { ListingChanges = detailListingId } = current
            match detailListingId with 
            | None -> current
            | Some _ -> processPageChange navigationDetails
                        {current with CurrentPage = ListingChangesPage}
        | About navigationDetails -> processPageChange navigationDetails
                                     {current with CurrentPage = CurrentPage.About}
        | Root -> Debug.WriteLine <| sprintf "Popping to root"
                  {current with CurrentPage = ListingsPage; ListingChanges = None; AddListingModel = None}
                
            
    let requestAction msg current = 
        match msg with 
        | RequstLoad    -> loadItems() |> ignore
                           current
        | RequstRefresh -> refreshListings current.Items
                           {current with IsRefreshing = true}
        | AddListingMessage x ->  validateListing x
                                  {current with AddListingModel = Some {OutputMessage = "Loading..."; ItemAdded = false; IsValidatingItem = true; EntryText = x}}
        | SetListingDetail  (navDetails, listingId) -> 
            let current = {current with ListingChanges = Some listingId}
            async { ListingDetail navDetails |> Msg.ChangePage |> this.Update } |> Async.Start
            current
        | DeleteListing listingId -> 
                                     current.Items |> List.filter (fun x -> x.Listing.ListingId = listingId) |> deleteListing <| listingId
                                     current
        | ToggleShowRemoved -> {current with ShowRemovedListings = not current.ShowRemovedListings }
                                
            
    let update (msg : Msg) (current : Model) = 
        match msg with
        | Msg.RequestAction m -> requestAction m current
        | Msg.RequestCompleted u -> handleUpdateItems u current
        | Msg.ChangePage cp -> handleChangePage cp current
        | Msg.Loop -> current

    let initialModel = {
        Items = []
        ShowRemovedListings = true 
        IsRefreshing = false
        AddListingModel = None
        ListingChanges = None
        CurrentPage = ListingsPage
    }

    let state = new AsyncMutable<Model>(initialModel)

    member __.Update msg = 
        update msg |> state.Update |> ignore

    member __.ToSignal () = state :> ISignal<_> 

    member __.Initlize () = 
        RequestAction RequstLoad |> this.Update
