module EA.Russian.Services.Kdmid.Service

open System
open EA.Russian.Services.DataAccess.Kdmid
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Russian.Services.Kdmid.Web
open EA.Russian.Services.Domain.Kdmid

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
                        ProcessState = r.Payload.State |> PayloadState.print |> Completed
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
        let setInitialState =
            ResultAsync.wrap (fun r ->
                {
                    r with
                        ProcessState = InProcess
                        Modified = DateTime.UtcNow
                }
                |> client.updateRequest)

        let createHttpClient =
            ResultAsync.bind (fun r ->
                r.Payload.Credentials.Subdomain
                |> client.initHttpClient
                |> Result.map (fun httpClient -> httpClient, r))

        let parseInitialPage =
            ResultAsync.bindAsync (fun (httpClient, r) ->
                let queryParams = r.Payload.Credentials |> Http.createQueryParams
                (httpClient, client.getInitialPage, client.getCaptcha, client.solveIntCaptcha)
                |> Html.InitialPage.parse queryParams
                |> ResultAsync.map (fun formData -> httpClient, r, queryParams, formData)
                |> ResultAsync.mapError (fun error ->
                    Operation {
                        Message = error.Message
                        Code = Constants.ErrorCode.INITIAL_PAGE_ERROR |> Custom |> Some
                    }))

        let parseValidationPage =
            ResultAsync.bindAsync (fun (httpClient, r, qp, fd) ->
                (httpClient, client.postValidationPage)
                |> Html.ValidationPage.parse qp fd
                |> ResultAsync.map (fun formDataMap -> httpClient, r, qp, formDataMap))

        let parseAppointmentsPage =
            ResultAsync.bindAsync (fun (httpClient, r, qp, fdm) ->
                (httpClient, client.postAppointmentsPage)
                |> Html.AppointmentsPage.parse qp fdm r
                |> ResultAsync.map (fun (fdm, r) -> httpClient, r, qp, fdm))

        let parseConfirmationPage =
            ResultAsync.bindAsync (fun (httpClient, r, qp, fdm) ->
                (httpClient, client.postConfirmationPage)
                |> Html.ConfirmationPage.parse qp fdm r)

        let setFinalState requestRes =
            client.updateRequest |> setFinalProcessState request requestRes

        // pipe
        request
        |> validateLimits
        |> setInitialState
        |> createHttpClient
        |> parseInitialPage
        |> parseValidationPage
        |> parseAppointmentsPage
        |> parseConfirmationPage
        |> setFinalState

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
