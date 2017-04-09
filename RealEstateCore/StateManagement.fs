namespace RealEstateCore
open System.Diagnostics
open System.Threading.Tasks
open Xamarin.Forms
open ListingDownloader
open Gjallarhorn
open Gjallarhorn.Bindable
open Gjallarhorn.XamarinForms

module Option = 

    let optionOr left right = 
        match (left, right) with 
            | Some a, Some b -> Some a
            | Some a, None ->   Some a
            | None, Some a ->   Some a
            | None, None ->     None 

    let (.||) l r = optionOr l r

open Option 

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
    ListingChanges: ListingId option
    CurrentPage: CurrentPage
}

type Update =
    | FetchItems of FullListing list
    | ItemSaved
    | RefrehedItems of FullListing list
    | ItemValidated of FullListing option
    | DeletedListing of ListingId
    | DeletedListingFailed of string
    
type NavigationDetails = {
    Page: unit -> Page
    Navigation: INavigation -> Page -> Task
    Binding: Component<Model,Msg>
}

and RequestAction = 
    | RequstLoad
    | RequestRefresh
    | AddListingMessage of string
    | SetListingDetail of (NavigationDetails * ListingId)
    | DeleteListing of ListingId
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
     

type StateManagement (navPage: NavigationPage, loadItemsInBackground: unit -> unit, saveListing, validateListingInBackground, refreshListings, deleteListingInBackground) as this =

    let listingValidated item (current: Model) = 

        Debug.WriteLine <| sprintf "Listing has been validated"
        match item, current.AddListingModel with 
        | Some listingItem, Some listingModel -> 
            listingItem |> saveListing |> function 
            | Left errorMessage -> {current with AddListingModel = Some {OutputMessage = errorMessage; ItemAdded = false; IsValidatingItem = false; EntryText = ""}}
            | Right () -> 
                        { current with Items = listingItem :: current.Items 
                                       AddListingModel = Some { listingModel with 
                                                                            OutputMessage = "Listing saved"; 
                                                                            ItemAdded = true; 
                                                                            IsValidatingItem = false }}
        | Some listingItem, None -> 
            listingItem |> saveListing |> function 
            | Left errorMessage -> {current with AddListingModel = Some {OutputMessage = errorMessage; ItemAdded = false; IsValidatingItem = false; EntryText = ""}}
            | Right () -> { current with          Items = listingItem :: current.Items 
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
                |> Seq.map (fun (key, xs) -> let image = xs |> Seq.fold (fun image x -> image .|| x.Image ) None
                                             xs |> Seq.map (fun x -> {x with Image = image} ) )
                |> Seq.concat 
                |> Seq.toList
                
            {current with Items = filteredItems }
                           
        | RefrehedItems xs -> {current with Items = xs @ current.Items; IsRefreshing = false}
        | ItemSaved -> {current with AddListingModel = None}
        | ItemValidated x -> listingValidated x current
        | DeletedListingFailed listingId -> Debug.WriteLine <| sprintf "Listing deleted: %s" listingId; current
        | DeletedListing listingId -> 
            Debug.WriteLine <| sprintf "Listing deleted: %A" listingId
            {current with Items = current.Items |> List.filter (fun x -> x.Listing.ListingId <> listingId) }
            
    let processPageChange navigationDetails = 
        let app = Framework.application this.ToSignal (fun () -> ()) this.Update navigationDetails.Binding
        Framework.changePage (navigationDetails.Navigation navPage.Navigation) app (navigationDetails.Page ())
    
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
                  System.GC.Collect () // Android
                  {current with CurrentPage = ListingsPage; ListingChanges = None; AddListingModel = None}

    let requestAction msg current = 
        match msg with 
        | RequstLoad    -> loadItemsInBackground() |> ignore
                           current
        | RequestRefresh -> refreshListings current.Items
                            {current with IsRefreshing = true}
        | AddListingMessage x ->  validateListingInBackground x
                                  {current with AddListingModel = Some {OutputMessage = "Loading..."; ItemAdded = false; IsValidatingItem = true; EntryText = x}}
        | SetListingDetail  (navDetails, listingId) -> {current with ListingChanges = Some listingId} |> handleChangePage (ListingDetail navDetails)
        | DeleteListing listingId -> 
                                     current.Items |> List.filter (fun x -> x.Listing.ListingId = listingId) |> deleteListingInBackground <| listingId
                                     current
        | ToggleShowRemoved -> {current with ShowRemovedListings = not current.ShowRemovedListings }
                                
            
    let update (msg : Msg) (current : Model) = 
        let { ListingChanges = detailListingId } = current
        Debug.WriteLine <| sprintf "Update message: %A, current item: %A" msg detailListingId
        match msg with
        | Msg.RequestAction m -> requestAction m current
        | Msg.RequestCompleted u -> handleUpdateItems u current
        | Msg.ChangePage cp -> handleChangePage cp current
        | Msg.Loop -> current

    let initialModel = {
        Items = []
        ShowRemovedListings = false 
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
