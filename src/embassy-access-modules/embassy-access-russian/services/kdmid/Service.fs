module EA.Russian.Services.Kdmid.Service

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Russian.Services.Kdmid.Web
open EA.Russian.Services.Domain.Kdmid

let private validate (request: Request<Payload>) =
    request.ValidateLimits()
    |> Result.mapError (fun error -> $"%s{error} Operation cancelled." |> Canceled)
    |> Result.bind (fun r ->
        match r.Payload.State with
        | NoAppointments
        | HasAppointments _ -> r |> Ok
        | HasConfirmation(d, _) ->
            $"The request has already been confirmed. %s{d}. Operation cancelled."
            |> Canceled
            |> Error)

let private setFinalProcessState (request: Request<Payload>) =
    fun updateRequest ->
        Async.bind (function
            | Ok(r: Request<Payload>) ->
                updateRequest {
                    r.UpdateLimits() with
                        Modified = DateTime.UtcNow
                        ProcessState = r.Payload.State.Print() |> Completed
                }
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
                            Payload = {
                                r.Payload with
                                    Confirmation =
                                        match r.Payload.Confirmation with
                                        | ForAppointment _ -> Disabled
                                        | confirmation -> confirmation
                            }
                    }
                    |> Async.map (function
                        | Ok _ -> Error error
                        | Error error -> Error error))

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
                (httpClient, client.getInitialPage, client.getCaptcha, client.solveCaptcha)
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

        let setFinalState = client.updateRequest |> setFinalProcessState request

        // pipe
        request
        |> validate
        |> setInitialState
        |> createHttpClient
        |> parseInitialPage
        |> parseValidationPage
        |> parseAppointmentsPage
        |> parseConfirmationPage
        |> setFinalState

let tryProcessFirst requests =
    fun (client: Client, handleResult) ->

        let rec processNextRequest (remainingRequests: Request<Payload> list) =
            async {
                match remainingRequests with
                | [] ->
                    return
                        "All of the attempts to get the first available result have been reached. Operation canceled."
                        |> Canceled
                        |> Error
                | request :: requestsTail ->
                    match! client |> tryProcess request with
                    | Error error ->
                        do! error |> Error |> handleResult
                        return! requestsTail |> processNextRequest
                    | Ok result ->
                        do! result |> Ok |> handleResult
                        return Ok result
            }

        requests |> List.ofSeq |> processNextRequest

let tryProcessAll requests =
    fun (client: Client, handleResult) ->

        let rec processNextRequest (remainingRequests: Request<Payload> list) =
            async {
                match remainingRequests with
                | [] -> return Ok()
                | request :: requestsTail ->
                    match! client |> tryProcess request with
                    | Error error ->
                        do! error |> Error |> handleResult
                        return! requestsTail |> processNextRequest
                    | Ok result ->
                        do! result |> Ok |> handleResult
                        return! requestsTail |> processNextRequest
            }

        requests |> List.ofSeq |> processNextRequest
