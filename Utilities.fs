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


module AList =

    let observe (action : IndexList<'a> -> IndexListDelta<'a> -> unit) (list : alist<'a>) =

        // here we setup a thread that blocks until 'changesPending' is set to true
        // by the use of a standard-monitor
        let trigger = obj()
        let mutable changesPending = true

        // a reader is a stateful "listener" on the list that can be queried for
        // pending changes. Much like IEnumerator<'a> for seq<'a>
        let reader = list.GetReader()

        // whenever there are changes to fetch trigger our thread
        let subscription = 
            reader.AddMarkingCallback (fun () ->
                lock trigger (fun () ->
                    changesPending <- true
                    Monitor.PulseAll trigger
                )
            )

        let mutable running = true

        startThread <| fun () ->
            while running do
                lock trigger (fun () ->
                    while not changesPending do
                        Monitor.Wait trigger |> ignore
                    changesPending <- false
                )
                if running then
                    let changes = reader.GetChanges AdaptiveToken.Top
                    // due to the way adaptive works internally there might be "dummy"-changes in certain scenarios
                    if not (IndexListDelta.isEmpty changes) then
                        let state = reader.State
                        action state changes
                    
        { new IDisposable with
            member x.Dispose() =
                subscription.Dispose()
                running <- false
                lock trigger (fun () -> changesPending <- true; Monitor.PulseAll trigger)
        }

    let toObservable (list : alist<'a>) =
        { new IObservable<list<'a>> with
            member x.Subscribe(obs : IObserver<list<'a>>) =
                list |> observe (fun s _ -> obs.OnNext (IndexList.toList s))
        }
