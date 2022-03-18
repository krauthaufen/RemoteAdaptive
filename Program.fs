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


let subscription = 
    dependentList |> AList.observe (fun state changes ->
        for index, operation in changes do
            match operation with
            | Set value -> 
                printfn "   set(%A, %A)" index value
            | Remove ->
                printfn "   rem(%A)" index

        printfn "   result: %0A" (IndexList.toList state)
    )

// here's some code to update the list via the command-line
//  `+<name>` inserts `name` at a random position in the list
//  `-<name>` removes a random occurance of `name` from the list
//  `quit` exits the program
mainLoop input