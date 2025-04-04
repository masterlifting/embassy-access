module EA.Russian.Clients.Kdmid.Service

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients
open EA.Core.Domain
open EA.Russian.Clients.Kdmid.Web
open EA.Russian.Clients.Domain.Kdmid

let private createPayload request =

    let create (uri: Uri) =
        match uri.Host.Split '.' with
        | hostParts when hostParts.Length < 3 ->
            $"Kdmid host: '%s{uri.Host}' is not supported." |> NotSupported |> Error
        | hostParts ->
            let payload = ResultBuilder()

            payload {
                let subdomain = hostParts[0]

                let! embassyId =
                    match Constants.SUPPORTED_SUB_DOMAINS |> Map.tryFind subdomain with
                    | Some id -> id |> Graph.NodeIdValue |> Ok
                    | None -> $"Kdmid subdomain: '%s{subdomain}' is not supported." |> NotSupported |> Error

                let! queryParams = uri |> Http.Route.toQueryParams

                let! id =
                    queryParams
                    |> Map.tryFind "id"
                    |> Option.map (function
                        | AP.IsInt id when id > 1000 -> id |> Ok
                        | _ -> "Kdmid payload 'ID' query parameter is not supported." |> NotSupported |> Error)
                    |> Option.defaultValue ("Kdmid payload 'ID' query parameter not found." |> NotFound |> Error)

                let! cd =
                    queryParams
                    |> Map.tryFind "cd"
                    |> Option.map (function
                        | AP.IsLettersOrNumbers cd -> cd |> Ok
                        | _ -> "Kdmid payload 'CD' query parameter is not supported." |> NotSupported |> Error)
                    |> Option.defaultValue ("Kdmid payload 'CD' query parameter not found." |> NotFound |> Error)

                let! ems =
                    queryParams
                    |> Map.tryFind "ems"
                    |> Option.map (function
                        | AP.IsLettersOrNumbers ems -> ems |> Some |> Ok
                        | _ -> "Kdmid payload 'EMS' query parameter is not supported." |> NotSupported |> Error)
                    |> Option.defaultValue (None |> Ok)

                return {
                    EmbassyId = embassyId
                    Subdomain = subdomain
                    Id = id
                    Cd = cd
                    Ems = ems
                }
            }

    let validate (payload: Payload) =
        match request.Service.Embassy.Id = payload.EmbassyId with
        | true -> Ok payload
        | false ->
            Error
            <| NotSupported
                $"The subdomain '{payload.Subdomain}' of the requested embassy '{request.Service.Embassy.ShortName}' is not supported."

    request.Service.Payload
    |> Http.Route.toUri
    |> Result.bind create
    |> Result.bind validate

let private validateLimitations request =
    request.Limitations
    |> Seq.tryPick (fun l ->
        match l.State with
        | Reached period ->
            $"Limit of attempts reached. Remaining period: '%s{period |> String.fromTimeSpan}'. The operation cancelled."
            |> Some
        | Start
        | Active _ -> None)
    |> Option.map (Canceled >> Error)
    |> Option.defaultValue (request |> Ok)

let private setCompletedState request =
    fun updateRequest ->
        let message =
            match request.Appointments.IsEmpty with
            | true -> "No appointments found"
            | false ->
                match request.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
                | [] -> $"Found appointments: %i{request.Appointments.Count}"
                | confirmations -> $"Found confirmations: %i{confirmations.Length}"

        updateRequest {
            request with
                ProcessState = Completed message
                Retries = 0u<attempts>
                Modified = DateTime.UtcNow
        }

let private trySetFailedState error request =
    fun updateRequest ->
        Request.updateLimitations {
            request with
                ProcessState = Failed error
                Modified = DateTime.UtcNow
        }
        |> updateRequest
        |> ResultAsync.bind (fun _ -> $"{Environment.NewLine}%s{request.Service.Payload}" |> error.Extend |> Error)

let rec tryProcess (request: Request) =
    fun (client: Client) ->

        // define
        let inline initProcessState r =
            {
                r with
                    ProcessState = InProcess
                    Modified = DateTime.UtcNow
            }
            |> client.updateRequest

        let createPayload =
            ResultAsync.bind (fun r -> r |> createPayload |> Result.map (fun payload -> r, payload))

        let createHttpClient =
            ResultAsync.bind (fun (r, p) ->
                p.Subdomain
                |> client.initHttpClient
                |> Result.map (fun httpClient -> httpClient, r, p))

        let parseInitialPage =
            ResultAsync.bindAsync (fun (httpClient, r, p) ->
                let queryParams = p |> Http.createQueryParams
                (httpClient, client.getInitialPage, client.getCaptcha, client.solveIntCaptcha)
                |> Html.InitialPage.parse queryParams
                |> ResultAsync.map (fun formData -> httpClient, r, queryParams, formData))

        let validateLimitations =
            ResultAsync.bind (fun (httpClient, r, qp, fd) ->
                r |> validateLimitations |> Result.map (fun r -> httpClient, r, qp, fd))

        let parseValidationPage =
            ResultAsync.bindAsync (fun (httpClient, r, qp, fd) ->
                (httpClient, client.postValidationPage)
                |> Html.ValidationPage.parse qp fd
                |> ResultAsync.map (fun formDataMap -> httpClient, r, qp, formDataMap)
                |> ResultAsync.mapErrorAsync (fun error ->
                    match error with
                    | Operation reason when reason.Code = Some(Custom Web.Captcha.ERROR_CODE) ->
                        match request.Retries > 3u<attempts> with
                        | true ->
                            "Limit of Captcha retries reached. The operation cancelled."
                            |> Canceled
                            |> Error
                            |> async.Return
                        | false ->
                            client
                            |> tryProcess {
                                request with
                                    Retries = request.Retries + 1u<attempts>
                            }))

        let parseAppointmentsPage =
            ResultAsync.bindAsync (fun (httpClient, r, qp, fdm) ->
                (httpClient, client.postAppointmentsPage)
                |> Html.AppointmentsPage.parse qp fdm r
                |> ResultAsync.map (fun (fdm, r) -> httpClient, r, qp, fdm))

        let parseConfirmationPage =
            ResultAsync.bindAsync (fun (httpClient, r, qp, fdm) ->
                (httpClient, client.postConfirmationPage)
                |> Html.ConfirmationPage.parse qp fdm r)

        let setFinalProcessState =
            Async.bind (function
                | Ok r -> client.updateRequest |> setCompletedState r
                | Error error -> client.updateRequest |> trySetFailedState error request)

        // pipe
        request
        |> initProcessState
        |> createPayload
        |> createHttpClient
        |> parseInitialPage
        |> validateLimitations
        |> parseValidationPage
        |> parseAppointmentsPage
        |> parseConfirmationPage
        |> setFinalProcessState

let tryProcessFirst (requests: Request seq) =
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

        let rec processNextRequest (errors: Error' list) (remainingRequests: Request list) =
            async {
                match remainingRequests with
                | [] -> return Error(List.rev errors)
                | request :: requestsTail ->
                    match! client |> tryProcess request with
                    | Error error ->
                        do!
                            match request |> Notification.tryCreateFail error errorFilter with
                            | None -> async.Return()
                            | Some notification -> notification |> notify

                        return! requestsTail |> processNextRequest (error :: errors)

                    | Ok result ->
                        do!
                            match result |> Notification.tryCreate errorFilter with
                            | None -> async.Return()
                            | Some notification -> notification |> notify

                        return Ok result
            }

        processNextRequest [] (requests |> List.ofSeq)
