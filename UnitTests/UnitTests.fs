module UnitTests

open RequestActor
open TestTaskFSREquest

open System.Threading
open System.Threading.Tasks
open System.Net.Http

open Xunit


[<Fact>]
let RequstTest () = 
    let url = "http://www.school93.kmr.ru/"
    let method = "GET"
    
    use client = new HttpClient()
    let requestMessage = new HttpRequestMessage(HttpMethod(method.ToUpper()), url)
    let response = client.Send(requestMessage)
    let expected = response.Content.ReadAsStringAsync()
    task{
        let! body = (DoRequest (new CancellationTokenSource()).Token url method)

        match body with
        | Ok result ->
            Assert.Equal(result, expected.Result)
        | Error result ->
            Assert.Fail()
    }

[<Fact>]
let BadRequestTest () = 
    let url = "https://bad_request_haha.org"
    let method = "GET"
    
    task{
        let! result = (DoRequest (new CancellationTokenSource()).Token url method)   
        
        match result with
        | Error result ->
            Assert.Equal(AppError.UnsuccessStatusCode, result)
        | Ok result ->
            Assert.Fail()
    }

[<Fact>]
let CancelTest () : Task<unit> =
    let url = "http://www.school93.kmr.ru/"
    let method = "GET"
    
    task{
        let token = new CancellationTokenSource()

        let request = (DoRequest token.Token url method)   
        
        token.Cancel()

        match request.Result with
        | Error res ->
            Assert.Equal(AppError.RequestIsCanceled, res)
        | Ok res ->
            Assert.Fail()
    }
    