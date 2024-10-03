open System.Net
open System.Net.Http
open System.Threading
open System.Threading.Tasks
open Akka.FSharp
open FsToolkit.ErrorHandling


[<RequireQualifiedAccess>]
type AppError = 
    | UnsuccessStatusCode
    static member ToMessage error = 
        match error with
        | UnsuccessStatusCode -> "Не удалось выполнить запрос"

[<RequireQualifiedAccess>]
type MessageType = 
    | Request of URL: string * Method: string
    | CancelRequest

let DoRequest listener (cancellationToken: CancellationToken) (url: string) (method: string) =
    taskResult{
        use client = new HttpClient()

        let requestMessage = new HttpRequestMessage(HttpMethod(method.ToUpper()), url)
        let! response = client.SendAsync(requestMessage, cancellationToken)

        do! response.StatusCode = HttpStatusCode.OK |> Result.requireTrue AppError.UnsuccessStatusCode
  
        let! body = response.Content.ReadAsStringAsync()
        listener <! body
    }
    |> TaskResult.teeError(fun error ->
            listener <! (error |> AppError.ToMessage))
    |> TaskResult.ignoreError
    :> Task

let Requester listener (mailbox: Actor<MessageType>) =
    let mutable token = new CancellationTokenSource()

    let rec loop() = actor {
        let! message = mailbox.Receive()

        Akka.Dispatch.ActorTaskScheduler.RunTask(fun () ->
        async{
                match message with
                | MessageType.Request(url, method) -> 
                    do (DoRequest listener token.Token url method) |> ignore
                | MessageType.CancelRequest ->
                    token.Cancel()
                    token <- new CancellationTokenSource()
        }
        |> Async.StartAsTask
        :> Task
        )

        return! loop()    
    }

    loop()

let Listener (mailbox: Actor<string>) =
    let rec loop() = actor {
        let! message = mailbox.Receive()

        match message with
        | msg -> 
            printf "%s" msg

        return! loop()
    }

    loop()

[<EntryPoint>]
let main argv =
    use system = System.create "RequestIO-system" (Configuration.load())
    let ListenerActor = spawn system "listener" Listener
    let RequestActor = spawn system "requester" (Requester ListenerActor)

    RequestActor <! MessageType.Request("https://google.com", "GET")
    RequestActor <! MessageType.CancelRequest
    RequestActor <! MessageType.Request("https://ya.ru", "GET")

    System.Console.ReadLine() |> ignore

    0


