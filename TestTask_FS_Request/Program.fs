open TestTaskFSREquest
open Akka.FSharp

open RequestActor
open ListenerActor


[<EntryPoint>]
let main argv =
    use system = System.create "RequestIO-system" (Configuration.load())
    let ListenerActor = spawn system "listener" Listener
    let RequestActor = spawn system "requester" (Requester ListenerActor)

    RequestActor <! MessageType.Request("https://google.com", "GET")
    RequestActor <! MessageType.CancelRequest
    RequestActor <! MessageType.Request("https://bad_request_haha.org", "GET")
    RequestActor <! MessageType.Request("https://google.com", "GET")

    System.Console.ReadLine() |> ignore

    0


