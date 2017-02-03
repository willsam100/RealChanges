//#r "../packages/FSharp.Data.2.3.2/lib/net40/FSharp.Data.dll"
//#load "ListingDownloader.fs"
#r "/Users/willsam100/Oooby/mobile-app/packages/FsCheck.2.6.2/lib/net45/FsCheck.dll"
//open ListingDownloader
//open FSharp.Data


#I "../packages/"
#r @"../packages/FsCheck.2.7.0/lib/net45/FsCheck.dll"
#r @"../packages/Hopac.0.3.21/lib/net45/Hopac.Core.dll"
#r @"../packages/Hopac.0.3.21/lib/net45/Hopac.Platform.dll"
#r @"../packages/Hopac.0.3.21/lib/net45/Hopac.dll"
#r @"../packages/Http.fs.4.1.0/lib/net40/HttpFs.dll"
//#r "../packages/"

open FsCheck
open System
open FsCheck
open Hopac
open Hopac.Core
open HttpFs.Client

let body =
  Request.createUrl Get "http://google.com"
  |> Request.responseAsString
  |> run

printfn "Here's the body: %s" body

type AppEvent = Work | Drive | Rest | FinishWork

let minVal = DateTime(0L) |> fun x -> x.Ticks
let maxVal = DateTime(0L) |> fun x -> x.AddDays 1. |> fun x -> x.Ticks

printfn "MinVal: %d, maxVal: %d" minVal maxVal

let dayGen xs = 
  gen { let! i = Gen.choose (0, List.length xs-1) 
        return List.item i xs }

let pickOne = Seq.unfold (let nextSecond x = x + (10L * 1000000L)
                          function
                           | st when (nextSecond st) > maxVal -> None
                           | st -> Some (st, nextSecond st) ) 
                        minVal


let matrix gen = Gen.sized <| fun s -> Gen.resize (s|>float|>sqrt|>int) gen

let genListEvents biasEvent = 
    let genNextEvent = function
        | Work ->       [Drive; Rest; FinishWork]
        | Drive ->      [Work; Rest; FinishWork]
        | Rest ->       [Drive; Work; FinishWork]
        | FinishWork -> [Drive; Rest; Work]

    let toGenNextEvent e = 

        let computeProbs xs = 
            xs |> List.contains biasEvent |> function
            | true -> (8, 1)
            | false -> (1, 1)

        let nextEvents = e |> genNextEvent 
        let probs = computeProbs nextEvents

        nextEvents |> List.map (fun x -> (if (x = biasEvent) then (fst <| probs) else (snd probs)), x)
        |> List.map (fun (x,y) -> (x, Gen.constant y)) 
        |> Gen.frequency

    let ininitalValue = Gen.constant FinishWork |> Gen.listOfLength 1
    let rec genListEventsInReverse (events: Gen<AppEvent list>) s =

        let growList () = 
            gen {
                let! v = events
                let lastValue = List.head v
                let! nextValue = toGenNextEvent lastValue
                return (nextValue :: v)
            } 

        let genListReverse gxs = 
            gen { 
                let! xs = gxs
                return xs |> List.rev
            }

        match (s/4) with 
        | s when s < 0 -> events |> genListReverse
        | 0 -> events |> genListReverse
        | s ->  genListEventsInReverse (growList ()) (s-1)

    Gen.sized <| genListEventsInReverse ininitalValue


let genListEventsWithTimes biasEvent maxWork maxRest = 
    let addMinutes x =
        let compute max = Gen.choose (0, max) |> Gen.map (fun y -> (x,y))
        match x with 
        | Drive | Work -> compute maxWork
        | Rest | FinishWork -> compute maxRest
    genListEvents biasEvent >>= (fun xs -> xs |> List.map addMinutes |> Gen.sequence)

type AppEventArb =
  static member AppEvents() =
      {new Arbitrary<List<AppEvent*int>>() with
          override x.Generator = genListEventsWithTimes Drive (11 * 60) 30 }


Arb.register<AppEventArb>()


let validEvents (xs: (AppEvent*int) list) = 
    xs |> List.pairwise |> List.forall (fun (x,y) -> (fst x) <> (fst y))

let biasIsCorrect (xs: (AppEvent * int) list) = 
    let countBias e = 
        xs |> List.fold (fun s x -> if (fst x = e) then s + 1 else s) 0

    let driveCount = countBias Drive 
    let workCount = countBias Work 
    let restCount = countBias Rest 
    let fwCount = countBias FinishWork 

    let givenContainsDriveEvent = List.fold (fun r x -> r || fst x = Drive) false xs

    givenContainsDriveEvent ==> 
        (driveCount + 1 >= workCount && 
            driveCount + 1 >= restCount && 
            driveCount + 2 >= fwCount)


let hasSufficientDriving (xs: (AppEvent * int) list) = 
    let hasEnoughDriving = 
        xs 
        |> List.fold (fun s (x,y) -> match x with 
                                     | Work | Drive -> s + y
                                     | Rest | FinishWork -> s) 0
        |> (fun x -> printfn "Total Driving Hours: %d" (x/60); x)
        |> (fun x -> x > (11 * 60))

    hasEnoughDriving ==> biasIsCorrect xs

printfn "Count: %d" <| Seq.length pickOne
Seq.head pickOne |> DateTime |> printfn "Head: %A"
Seq.last pickOne |> DateTime |> printfn "Last: %A"
