#r "../packages/FSharp.Data.2.3.2/lib/net40/FSharp.Data.dll"

open FSharp.Data

let run () = 
    let data = System.IO.File.ReadAllText("/Users/willsam100/Desktop/list.html")
    let body = HtmlDocument.Parse(data) //|> Async.RunSynchronously

    //let tableWithPrice = "ListingAttributes"
    //let tableRows = "tr"

    let header = 
        body.Descendants ["h1"] 
        |> Seq.choose (fun x -> x.TryGetAttribute("itemprop") |> Option.map (fun a -> a.Value(), x)) 
        |> Seq.filter (fun (x, y) -> x = "name")
        |> Seq.map (fun (x,y) -> y.InnerText().Trim())
        |> Seq.tryHead
        
    let askingPrice = 
        body.Descendants ["h2"] 
        |> Seq.map (fun (x) -> x.InnerText().Trim())
        |> Seq.tryHead

    let image = 
        body.Descendants ["div"]
        |> Seq.choose (fun x -> x.TryGetAttribute("id") |> Option.map (fun a -> a.Value(), x)) 
        |> Seq.filter (fun (x, y) -> x = "mainImageHolder")
        |> Seq.map (fun (x,y) -> y.Descendants ["img"])
        |> Seq.concat
        |> Seq.choose (fun x -> x.TryGetAttribute "src" |> Option.map (fun y -> y.Value ()))
        |> Seq.tryHead
        
        
    let views = 
        body.Descendants ["span"]
        |> Seq.choose (fun x -> x.TryGetAttribute("class") |> Option.map (fun a -> a.Value(), x)) 
        |> Seq.filter (fun (x, y) -> x = "stats")
        |> Seq.map (fun (x,y) -> y.Descendants ["a"])
        |> Seq.concat
        |> Seq.choose (fun x -> x.TryGetAttribute "href" |> Option.map (fun a -> x))
        |> Seq.map (fun x -> x.InnerText())
        |> Seq.filter (fun x -> x.Contains "Listing Views")
        |> Seq.tryHead
        |> Option.map (fun x -> x.Replace("Listing Views", "").Trim())
        
    let listingId = 
        body.Descendants ["h4"]
        |> Seq.map (fun x -> x.Descendants ["b"])
        |> Seq.concat
        |> Seq.map (fun x -> x.InnerText())
        |> Seq.filter (fun x -> x.Contains "Listing")
        |> Seq.tryHead
        |> Option.map (fun x -> x.Replace("Listing", "").Replace("#", "").Trim())
        
        
        

//let run () = 
//    let data = System.IO.File.ReadAllText("/Users/willsam100/Desktop/list 4.html")
//    let body = HtmlDocument.Parse(data) //|> Async.RunSynchronously

//    let links = body.Descendants ["p"]
//                //|> Seq.choose (fun x -> x.TryGetAttribute "href" |> Option.map(fun y -> y.Value(), x))
//                |> Seq.map (fun (x) -> x.InnerText())
//                |> Seq.map (fun x -> x = "This listing was withdrawn by the administrator")
//                |> Seq.fold (fun x y -> x || y) false
//    //            |> Seq.filter (fun x -> x.Contains("?id="))
//    //            |> Seq.map (fun x -> x.Replace("/Browse/Listing.aspx?id=", ""))

//    //links |> Seq.iter (printfn "%A")
    printfn "header %A" listingId
run ()