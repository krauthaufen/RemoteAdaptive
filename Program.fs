open System
open System.Threading
open System.IO
open FSharp.Data.Adaptive
open Utilities

// let's start with an input-list
let input = clist ["a"; "b"]


// we create a dependent list appending its string-length to every element and truncating it to 10 elements
let dependentList = 
    input
    |> AList.map (fun el -> sprintf "%s:%d" el el.Length)
    |> AList.pairwise



// here we setup a thread that blocks until 'changesPending' is set to true
// by the use of a standard-monitor
let trigger = obj()
let mutable changesPending = true

// a reader is a stateful "listener" on the list that can be queried for
// pending changes. Much like IEnumerator<'a> for seq<'a>
let reader = dependentList.GetReader()

// whenever there are changes to fetch trigger our thread
let subscription = 
    reader.AddMarkingCallback (fun () ->
        lock trigger (fun () ->
            changesPending <- true
            Monitor.PulseAll trigger
        )
    )

startThread <| fun () ->
    while true do
        lock trigger (fun () ->
            while not changesPending do
                Monitor.Wait trigger |> ignore
            changesPending <- false
        )
        
        let changes = reader.GetChanges AdaptiveToken.Top
        if not (IndexListDelta.isEmpty changes) then
            let state = reader.State

            for index, operation in changes do
                match operation with
                | Set value -> 
                    printfn "   set(%A, %A)" index value
                | Remove ->
                    printfn "   rem(%A)" index

            printfn "   result: %0A" (IndexList.toList state)


// here's some code to update the list via the command-line
//  `+<name>` inserts `name` at a random position in the list
//  `-<name>` removes a random occurance of `name` from the list
//  `quit` exits the program
mainLoop input