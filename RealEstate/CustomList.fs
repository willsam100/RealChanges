namespace Re.Core

open System
open System.Windows.Input
open Xamarin.Forms

type ListView() as this = 
    inherit Xamarin.Forms.ListView()

    //let OnItemTapped(object sender, ItemTappedEventArgs e) = 
    //    if (e.Item != null && this.ItemClickCommand != null && this.ItemClickCommand.CanExecute(e)) then 
    //        this.ItemClickCommand.Execute(e.Item)
    //        this.SelectedItem = null
                
    //static let mutable ItemClickCommandProperty: BindableProperty = BindableProperty.Create<ListView, ICommand>(x => x.ItemClickCommand, null);
    //member this.ItemClickCommand //: ICommand 
    //    with get() (ICommand)this.GetValue(ItemClickCommandProperty)
    //    and set() this.SetValue(ItemClickCommandProperty, value)

    //do 
    //    this.ItemTapped += this.OnItemTapped

//namespace YourNS {

//    public class ListView : Xamarin.Forms.ListView {

//        public static BindableProperty ItemClickCommandProperty = BindableProperty.Create<ListView, ICommand>(x => x.ItemClickCommand, null);


//        public ListView() {
//            this.ItemTapped += this.OnItemTapped;
//        }


//        public ICommand ItemClickCommand {
//            get { return (ICommand)this.GetValue(ItemClickCommandProperty); }
//            set { this.SetValue(ItemClickCommandProperty, value); }
//        }


//        private void OnItemTapped(object sender, ItemTappedEventArgs e) {
//            if (e.Item != null && this.ItemClickCommand != null && this.ItemClickCommand.CanExecute(e)) {
//                this.ItemClickCommand.Execute(e.Item);
//                this.SelectedItem = null;
//            }
//        }
//    }
//}