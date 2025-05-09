module EA.Italian.Services.Prenotami.Service

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain
open EA.Core.Domain
open EA.Italian.Services.Prenotami.Client
open EA.Italian.Services.Domain.Prenotami

let private validateLimits (request: Request<Payload>) =
    request.ValidateLimits()
    |> Result.mapError (fun error -> $"{error} The operation cancelled." |> Canceled)

let private processWebSite credentials (client: Client) =
    //define
    let loadInitialPage () =
        client.Browser.initProvider ()
        |> ResultAsync.bindAsync (client.Browser.loadPage ("https://prenotami.esteri.it" |> Uri))

    let setLogin page =
        page
        |> client.Browser.fillInput (Browser.Selector "//input[@id='login-email']") credentials.Login

    let setPassword page =
        page
        |> client.Browser.fillInput (Browser.Selector "//input[@id='login-password']") credentials.Password

    let submitForm page =
        page
        |> client.Browser.executeCommand (Browser.Selector "form#login-form") "form => form.submit()"
        
    let clickBook page =
        page
        |> client.Browser.clickButton (Browser.Selector "//a[@id='advanced']")

    // pipe
    loadInitialPage ()
    |> ResultAsync.bindAsync setLogin
    |> ResultAsync.bindAsync setPassword
    |> ResultAsync.bindAsync client.Browser.mouseShuffle
    |> ResultAsync.bindAsync submitForm

let private setFinalProcessState (request: Request<Payload>) requestPipe =
    
    fun updateRequest ->
        requestPipe
        |> Async.bind (function
            | Ok(r: Request<Payload>) ->
                r.UpdateLimits()
                |> fun r -> {
                    r with
                        Modified = DateTime.UtcNow
                        ProcessState = r.Payload |> Payload.print |> Completed
                }
                |> updateRequest
            | Error error ->
                match error with
                | Operation reason ->
                    match reason.Code with
                    | Some(Custom Constants.ErrorCode.INITIAL_PAGE_ERROR) -> request
                    | _ -> request.UpdateLimits()
                | _ -> request.UpdateLimits()
                |> fun r ->
                    updateRequest {
                        r with
                            ProcessState = Failed error
                            Modified = DateTime.UtcNow
                    })

let tryProcess (request: Request<Payload>) =
    fun (client: Client) ->

        // define
        let setInitialProcessState =
            ResultAsync.wrap (fun r ->
                {
                    r with
                        ProcessState = InProcess
                        Modified = DateTime.UtcNow
                }
                |> client.updateRequest)

        let processWebSite =
            ResultAsync.bindAsync (fun r ->
                client
                |> processWebSite r.Payload.Credentials
                |> ResultAsync.map (fun _ -> r)
                |> ResultAsync.mapError (fun error ->
                    Operation {
                        Message = error.Message
                        Code = Constants.ErrorCode.PAGE_HAS_ERROR |> Custom |> Some
                    }))

        let setFinalProcessState requestRes =
            client.updateRequest |> setFinalProcessState request requestRes

        // pipe
        request
        |> validateLimits
        |> setInitialProcessState
        |> processWebSite
        |> setFinalProcessState

let tryProcessFirst requests =
    fun (client: Client, notify) ->

        let rec processNextRequest (remainingRequests: Request<Payload> list) =
            async {
                match remainingRequests with
                | [] ->
                    return
                        "All of the attempts to get the first available result have been reached. The Operation canceled."
                        |> Canceled
                        |> Error
                | request :: requestsTail ->
                    match! client |> tryProcess request with
                    | Error error ->
                        do! error |> Error |> notify
                        return! requestsTail |> processNextRequest
                    | Ok result ->
                        do! result |> Ok |> notify
                        return Ok result
            }

        requests |> List.ofSeq |> processNextRequest
