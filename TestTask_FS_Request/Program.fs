open Akka.FSharp
open System.Net
open System.Net.Http
open System.Threading.Tasks
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

let DoRequest (url: string) (method: string) =
    taskResult{
        use client = new HttpClient()
        let httpMethod = HttpMethod(method)
        let requestMessage = new HttpRequestMessage(httpMethod, url)

        let! response = client.SendAsync(requestMessage)
        
        do! response.StatusCode = HttpStatusCode.OK |> Result.requireTrue AppError.UnsuccessStatusCode
        
        let! body = response.Content.ReadAsStringAsync()
        printfn "%s" body
    }
    |> Async.AwaitTask


[<EntryPoint>]
let main argv =
    use system = System.create "RequestIO-system" (Configuration.load())
    
    let RequestActor = spawn system "requester" <| fun mailbox ->
            let rec loop() = actor{
                let! message = mailbox.Receive()
                Akka.Dispatch.ActorTaskScheduler.RunTask(fun () ->
                taskResult{
                     match message with
                     | MessageType.Request(url, method) -> do! DoRequest url method

                     
                }
                |> TaskResult.teeError(fun error ->
                    printfn "%s" (error |> AppError.ToMessage))
                |> TaskResult.ignoreError
                :> Task
                )

                return! loop()    
            }
            loop()

    RequestActor <! MessageType.Request("https://google.com/", "GET")
    
    0


