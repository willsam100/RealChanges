#r "../packages/FSharp.Data.2.3.2/lib/net40/FSharp.Data.dll"

open FSharp.Data



//let run () = 
//    let data = System.IO.File.ReadAllText("/Users/willsam100/Desktop/list.html")
//    let body = HtmlDocument.Parse(data) //|> Async.RunSynchronously

    //let tableWithPrice = "ListingAttributes"
    //let tableRows = "tr"

    //let header = 
    //    body.Descendants ["h1"] 
    //    |> Seq.choose (fun x -> 
    //       x.TryGetAttribute("id") |> Option.map (fun a -> x.InnerText())) 
    //    |> Seq.map (fun x -> x.Trim()) 
    //    |> Seq.toList
    //    |> (fun x -> match x.Length with 
    //                  | 1 -> Some (x.Head)
    //                  | _ -> None )

    //let askingPrice = 
    //    body.Descendants ["table"] 
    //    |> Seq.choose (fun x -> x.TryGetAttribute("id") |> Option.map (fun a -> a.Value(), x)) 
    //    |> Seq.filter (fun (x, y) -> x = tableWithPrice)
    //    |> Seq.map (fun (x, y) -> y.Descendants [tableRows])
    //    |> Seq.concat
    //    |> Seq.map (fun x -> (x.Descendants ["th"]), (x.Descendants "td"))
    //    |> Seq.map (fun (x, y) -> (x |>  Seq.map (fun a -> a.InnerText()), y |> Seq.map (fun a -> a.InnerText())))
    //    |> Seq.filter (fun (x, y) -> (x |>  Seq.exists (fun a -> a.ToLower().Contains "price")))
    //    |> Seq.map (fun (x, y) -> y)
    //    |> Seq.concat
    //    |> Seq.map (fun x -> x.Trim())
    //    |> Seq.toList
    //    |> (fun x -> match x.Length with 
    //                  | 1 -> Some (x.Head)
    //                  | _ -> None )

    //let image = 
    //    body.Descendants ["img"]
    //    |> Seq.choose (fun x -> x.TryGetAttribute "src" |> Option.map (fun y -> (y.Value(), x)))
    //    |> Seq.choose (fun (a, x) -> x.TryGetAttribute "id" |> Option.map (fun y ->(a, y.Value(), x)))
    //    |> Seq.filter (fun (a, x, y) -> x = "mainImage")
    //    |> Seq.map (fun (a, x, y) -> a)

let run () = 
    let data = System.IO.File.ReadAllText("/Users/willsam100/Desktop/list 4.html")
    let body = HtmlDocument.Parse(data) //|> Async.RunSynchronously

    let links = body.Descendants ["h1"]
                //|> Seq.choose (fun x -> x.TryGetAttribute "href" |> Option.map(fun y -> y.Value(), x))
                |> Seq.map (fun (x) -> x.InnerText())
                |> Seq.map (fun x -> x = "Sorry, this classified has expired.")
                |> Seq.fold (fun x y -> x || y) false
    //            |> Seq.filter (fun x -> x.Contains("?id="))
    //            |> Seq.map (fun x -> x.Replace("/Browse/Listing.aspx?id=", ""))

    links |> Seq.iter (printfn "%A")

run ()