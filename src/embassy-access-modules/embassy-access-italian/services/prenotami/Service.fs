module EA.Italian.Services.Prenotami.Service

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Italian.Services.Prenotami.Web
open EA.Italian.Services.Domain.Prenotami

let private validateLimits (request: Request<Payload>) =
    request.ValidateLimits()
    |> Result.mapError (fun error -> $"{error} The operation cancelled." |> Canceled)

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

        let initBrowserProvider =
            ResultAsync.bindAsync (fun r ->
                client.Browser.initProvider ()
                |> ResultAsync.map (fun browserProvider -> browserProvider, r))

        let parseInitialPage =
            ResultAsync.bindAsync (fun (browserProvider, r) ->
                let loadPage uri =
                    browserProvider |> client.Browser.loadPage uri
                let waitPage = client.Browser.waitPage
                let fillInput = client.Browser.fillInput
                let clickButton = client.Browser.clickButton

                (loadPage, waitPage, fillInput, clickButton)
                |> Html.InitialPage.parse r.Payload.Credentials
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
        |> initBrowserProvider
        |> parseInitialPage
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
