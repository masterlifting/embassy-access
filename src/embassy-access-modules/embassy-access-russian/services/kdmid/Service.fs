module EA.Russian.Services.Kdmid.Service

open System
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
            | Ok (r: Request<Payload>) ->
                r.UpdateLimits()
                |> fun r -> {
                    r with
                        Modified = DateTime.UtcNow
                        ProcessState =
                            match r.Payload.Appointments.IsEmpty with
                            | true -> "No appointments found"
                            | false ->
                                match r.Payload.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
                                | [] -> $"Found appointments: %i{r.Payload.Appointments.Count}"
                                | confirmations -> $"Found confirmations: %i{confirmations.Length}"
                            |> Completed
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

        let setFinalProcessState requestRes =
            client.updateRequest |> setFinalProcessState request requestRes

        // pipe
        request
        |> validateLimits
        |> setInitialProcessState
        |> createHttpClient
        |> parseInitialPage
        |> parseValidationPage
        |> parseAppointmentsPage
        |> parseConfirmationPage
        |> setFinalProcessState

let tryProcessFirst (requests: Request<Payload> seq) =
    fun (client: Client, notify) ->

        let inline errorFilter error =
            match error with
            | Operation reason ->
                match reason.Code with
                | Some(Custom Constants.ErrorCode.REQUEST_AWAITING_LIST)
                | Some(Custom Constants.ErrorCode.REQUEST_NOT_CONFIRMED)
                | Some(Custom Constants.ErrorCode.REQUEST_BLOCKED)
                | Some(Custom Constants.ErrorCode.REQUEST_NOT_FOUND)
                | Some(Custom Constants.ErrorCode.REQUEST_DELETED) -> true
                | _ -> false
            | _ -> false

        let rec processNextRequest (errors: Error' list) (remainingRequests: Request<Payload> list) =
            async {
                match remainingRequests with
                | [] -> return Error(List.rev errors)
                | request :: requestsTail ->
                    match! client |> tryProcess request with
                    | Error error ->
                        do! request |> notify
                        return! requestsTail |> processNextRequest (error :: errors)

                    | Ok result ->
                        do! result |> notify
                        return Ok result
            }

        processNextRequest [] (requests |> List.ofSeq)
