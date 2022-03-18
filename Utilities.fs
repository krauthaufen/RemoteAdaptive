module Utilities

open System
open System.Threading
open FSharp.Data.Adaptive

let rand = Random()

let startThread (action : unit -> unit) =
    let start = ThreadStart(action)
    let thread = Thread(start, IsBackground = true)
    thread.Start()

let err fmt = 
    fmt |> Printf.kprintf (fun str ->
        let o = Console.ForegroundColor
        try
            Console.ForegroundColor <- ConsoleColor.DarkRed
            Console.WriteLine str
        finally
            Console.ForegroundColor <- o
    )

let mainLoop(input : clist<string>) =
    while true do
        let line = Console.ReadLine().Trim()
        
        if line.StartsWith "+" then 
            let position = rand.Next(input.Count + 1)
            transact (fun () -> input.InsertAt(position, line.Substring 1) |> ignore)
        elif line.StartsWith "-" then   
            let elem = line.Substring 1
            let indices = input.Value |> IndexList.toArrayIndexed |> Array.choose (fun (idx, value) -> if value = elem then Some idx else None)
            if indices.Length > 0 then
                let idx = indices.[rand.Next(indices.Length)]
                transact (fun () -> input.Remove idx |> ignore)
            else
                err "could not find element %A" elem
        elif line = "quit" then
            printfn "bye!"
            exit 0
        else
            err "bad command: %A" line
