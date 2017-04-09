namespace RealEstateCore
open Xamarin.Forms
open Xamarin.Forms.Xaml
open FFImageLoading.Forms
open System

type NavigationWithBehaviour(page: Page) as this = 
    inherit NavigationPage(page)
    do this.LoadFromXaml(typeof<NavigationWithBehaviour>) |> ignore
       this.Title <- page.Title

type Listings() as this = 
    inherit ContentPage() 
    do this.LoadFromXaml(typeof<Listings>) |> ignore
       

type SoldListings() as this = 
    inherit ContentPage() 
    do this.LoadFromXaml(typeof<SoldListings>) |> ignore
       this.Title <- "Sold"
        
type AboutPage() as this = 
    inherit ContentPage() 
    let icon () = FileImageSource(File = "Feedback")

    do this.LoadFromXaml(typeof<AboutPage>) |> ignore
       this.Title <- "Feedback"

type ListingChanges() as this = 
    inherit ContentPage() 
    do this.LoadFromXaml(typeof<ListingChanges>) |> ignore   
    let image = this.FindByName<CachedImage>("image")  

type AddListing() as this = 
    inherit ContentPage() 
    do this.LoadFromXaml(typeof<AddListing>) |> ignore
    
    let entry = this.FindByName<Entry>("entry")

    override this.OnAppearing() = 
        base.OnAppearing ()
        entry.Focus () |> ignore
        

        
