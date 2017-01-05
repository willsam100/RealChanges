namespace ReCoreVerTwo
open Xamarin.Forms
open Xamarin.Forms.Xaml
open System.Diagnostics
open System.Windows.Input

type NavigationWithBehaviour(page: Page) as this = 
    inherit NavigationPage(page)
    
    do
        this.LoadFromXaml(typeof<NavigationWithBehaviour>) |> ignore

type Listings() as this = 
    inherit ContentPage() 

    do
        this.LoadFromXaml(typeof<Listings>) |> ignore
        
type About() as this = 
    inherit ContentPage() 

    do
        this.LoadFromXaml(typeof<About>) |> ignore

type ListingChanges() as this = 
    inherit ContentPage() 
    
    do
        this.LoadFromXaml(typeof<ListingChanges>) |> ignore
        
        

type AddListing() as this = 
    inherit ContentPage() 

    let _ = this.LoadFromXaml(typeof<AddListing>)
    let entry = this.FindByName<Entry>("entry")
    do Debug.WriteLine <| sprintf "Creating new AddListing page %A" (this.ToString())

    override this.OnAppearing() = 
        entry.Focus () |> ignore
        Debug.WriteLine <| sprintf "Add listing page appearing %A" (this.ToString())
        
    override this.OnDisappearing() =
        Debug.WriteLine <| sprintf "Add listing page disappearing %A" (this.ToString())
        
    override this.OnBindingContextChanged() = 
        Debug.WriteLine <| sprintf "Add listing binding context changed %s" (this.ToString())
