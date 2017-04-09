//#load "ListingDownloader.fs"
//open ListingDownloader
#r "../packages/FSharp.Data.2.3.2/lib/net40/FSharp.Data.dll"
open FSharp.Data
//open FsCheck
open System
//open FsCheck

open System.Reflection


type Property = HtmlProvider<"http://www.trademe.co.nz/property/residential-property-for-sale/auction-1297643730.htm">
//let sample = Property.GetSample()

type Nuget = HtmlProvider<"https://www.nuget.org/packages/Spreads/0.8.0-build1703210723">

type Superrugby = HtmlProvider<"http://www.lassen.co.nz/s14tab.php#hrh">

type Cars = HtmlProvider<"https://www.avis.co.nz/car-rental/reservation/time-place-submit.ac">

let sample = Cars.GetSample()

let test (package: string) = 
   let sample = Nuget.Load package
   sample.Tables.``Version History``.Rows |> Array.iter (printfn "%A")


let printRows x = 
    x|> Array.iter (printfn "%A")

let rugby (page: string) =  
    let sample = Superrugby.Load page
    sample.Tables.``2017 Super Rugby Table (After Week 6)``.Rows |> printRows



let run (site: string) = 
    let sample = Property.Load site

    sample.Lists.``Grant Chappell``.Values |> printfn "%A"
    sample.Tables.ListingAttributes.Rows |> Array.iter (printfn "%A")
    //sample.Lists.``Recent QV.co.nz sales information``.Values |> (printfn "%A")
    //sample.Lists.``Matty Ma``.Name |> (printfn "%A")
    sample.Lists.Html.Descendants ["h1"] 
    |> Seq.choose (fun x -> 
       x.TryGetAttribute("id") |> Option.map (fun a -> x.InnerText()))  |> (printfn "%A")

    //sample.Lists.List15.Values |> (printfn "%A")




let calc x rate = 
    ((x * 1.60934) / rate) * 0.264172 * 2.7 * 1.44





//type AppEvent = Work | Drive | Rest | FinishWork

//let minVal = DateTime(0L) |> fun x -> x.Ticks
//let maxVal = DateTime(0L) |> fun x -> x.AddDays 1. |> fun x -> x.Ticks

//printfn "MinVal: %d, maxVal: %d" minVal maxVal

//let dayGen xs = 
//  gen { let! i = Gen.choose (0, List.length xs-1) 
//        return List.item i xs }

//let pickOne = Seq.unfold (let nextSecond x = x + (10L * 1000000L)
//                          function
//                           | st when (nextSecond st) > maxVal -> None
//                           | st -> Some (st, nextSecond st) ) 
//                        minVal


//let matrix gen = Gen.sized <| fun s -> Gen.resize (s|>float|>sqrt|>int) gen

//let genListEvents biasEvent = 
//    let genNextEvent = function
//        | Work ->       [Drive; Rest; FinishWork]
//        | Drive ->      [Work; Rest; FinishWork]
//        | Rest ->       [Drive; Work; FinishWork]
//        | FinishWork -> [Drive; Rest; Work]

//    let toGenNextEvent e = 

//        let computeProbs xs = 
//            xs |> List.contains biasEvent |> function
//            | true -> (8, 1)
//            | false -> (1, 1)

//        let nextEvents = e |> genNextEvent 
//        let probs = computeProbs nextEvents

//        nextEvents |> List.map (fun x -> (if (x = biasEvent) then (fst <| probs) else (snd probs)), x)
//        |> List.map (fun (x,y) -> (x, Gen.constant y)) 
//        |> Gen.frequency

//    let ininitalValue = Gen.constant FinishWork |> Gen.listOfLength 1
//    let rec genListEventsInReverse (events: Gen<AppEvent list>) s =

//        let growList () = 
//            gen {
//                let! v = events
//                let lastValue = List.head v
//                let! nextValue = toGenNextEvent lastValue
//                return (nextValue :: v)
//            } 

//        let genListReverse gxs = 
//            gen { 
//                let! xs = gxs
//                return xs |> List.rev
//            }

//        match (s/4) with 
//        | s when s < 0 -> events |> genListReverse
//        | 0 -> events |> genListReverse
//        | s ->  genListEventsInReverse (growList ()) (s-1)

//    Gen.sized <| genListEventsInReverse ininitalValue


//let genListEventsWithTimes biasEvent maxWork maxRest = 
//    let addMinutes x =
//        let compute max = Gen.choose (0, max) |> Gen.map (fun y -> (x,y))
//        match x with 
//        | Drive | Work -> compute maxWork
//        | Rest | FinishWork -> compute maxRest
//    genListEvents biasEvent >>= (fun xs -> xs |> List.map addMinutes |> Gen.sequence)

//type AppEventArb =
//  static member AppEvents() =
//      {new Arbitrary<List<AppEvent*int>>() with
//          override x.Generator = genListEventsWithTimes Drive (11 * 60) 30 }


//Arb.register<AppEventArb>()




//let validEvents (xs: (AppEvent*int) list) = 
//    xs |> List.pairwise |> List.forall (fun (x,y) -> (fst x) <> (fst y))

//let biasIsCorrect (xs: (AppEvent * int) list) = 
//    let countBias e = 
//        xs |> List.fold (fun s x -> if (fst x = e) then s + 1 else s) 0

//    let driveCount = countBias Drive 
//    let workCount = countBias Work 
//    let restCount = countBias Rest 
//    let fwCount = countBias FinishWork 

//    let givenContainsDriveEvent = List.fold (fun r x -> r || fst x = Drive) false xs

//    givenContainsDriveEvent ==> 
//        (driveCount + 1 >= workCount && 
//            driveCount + 1 >= restCount && 
//            driveCount + 2 >= fwCount)


//let hasSufficientDriving (xs: (AppEvent * int) list) = 
//    let hasEnoughDriving = 
//        xs 
//        |> List.fold (fun s (x,y) -> match x with 
//                                     | Work | Drive -> s + y
//                                     | Rest | FinishWork -> s) 0
//        |> (fun x -> printfn "Total Driving Hours: %d" (x/60); x)
//        |> (fun x -> x > (11 * 60))

//    hasEnoughDriving ==> biasIsCorrect xs

//printfn "Count: %d" <| Seq.length pickOne
//Seq.head pickOne |> DateTime |> printfn "Head: %A"
//Seq.last pickOne |> DateTime |> printfn "Last: %A"
