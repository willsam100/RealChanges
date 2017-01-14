namespace RealEstateCore
open Xamarin.Forms
open Xamarin.Forms.Xaml

type NavigationWithBehaviour(page: Page) as this = 
    inherit NavigationPage(page)
    do this.LoadFromXaml(typeof<NavigationWithBehaviour>) |> ignore

type Listings() as this = 
    inherit ContentPage() 
    do this.LoadFromXaml(typeof<Listings>) |> ignore
        
type About() as this = 
    inherit ContentPage() 
    do this.LoadFromXaml(typeof<About>) |> ignore

type ListingChanges() as this = 
    inherit ContentPage() 
    do this.LoadFromXaml(typeof<ListingChanges>) |> ignore        

type AddListing() as this = 
    inherit ContentPage() 
    do this.LoadFromXaml(typeof<AddListing>) |> ignore
    
    let entry = this.FindByName<Entry>("entry")

    override this.OnAppearing() = 
        base.OnAppearing ()
        entry.Focus () |> ignore
        

        
