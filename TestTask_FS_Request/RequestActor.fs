module RequestActor

open TestTaskFSREquest

open System.Net.Http
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open FsToolkit.ErrorHandling

open Akka.FSharp


let createNewToken (list: List<CancellationTokenSource>) = 
    let token = new CancellationTokenSource()
    list.Add(token)

    token

let cancelTokens (list: List<CancellationTokenSource>) = 
    for token in list do
        token.Cancel()

    list.Clear()

let DoRequest (cancellationToken: CancellationToken) (url: string) (method: string) =
    taskResult{
        use client = new HttpClient()

        let requestMessage = new HttpRequestMessage(HttpMethod(method.ToUpper()), url)

        try
            let! response = client.SendAsync(requestMessage, cancellationToken)
            let! body = response.Content.ReadAsStringAsync()
            return body
        with
        | :? TaskCanceledException as ex ->
            return! AppError.RequestIsCanceled |> Error
        | _ as ex ->
            return! AppError.UnsuccessStatusCode |> Error // Добавление в лог
    }

let sendBodyToListener listener (cancellationToken: CancellationToken) (url: string) (method: string) = 
    task{
        let! body = (DoRequest cancellationToken url method)

        match body with
        | Ok result -> 
            listener <! result
        | Error result ->
            printf "%s\n" (result |> AppError.ToMessage) // лог ошибки
    }

let Requester listener (mailbox: Actor<MessageType>) =
    let tokensList = new List<CancellationTokenSource>()

    let rec loop() = actor {
        let! message = mailbox.Receive()

        Akka.Dispatch.ActorTaskScheduler.RunTask(fun () ->
        async{
                match message with
                | MessageType.Request(url, method) ->
                    do (sendBodyToListener listener (createNewToken(tokensList).Token) url method) |> ignore
                | MessageType.CancelRequest ->
                    cancelTokens(tokensList)
        }
        |> Async.StartAsTask
        :> Task
        )

        return! loop()    
    }

    loop()
