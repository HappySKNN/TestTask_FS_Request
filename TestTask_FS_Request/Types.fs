namespace TestTaskFSREquest

open FSharp.Core


[<RequireQualifiedAccess>]
type AppError = 
    | UnsuccessStatusCode
    | RequestIsCanceled
    static member ToMessage error = 
        match error with
        | UnsuccessStatusCode -> "Не удалось выполнить запрос"
        | RequestIsCanceled -> "Запрос отменен"
    
[<RequireQualifiedAccess>]
type MessageType = 
    | Request of URL: string * Method: string
    | CancelRequest