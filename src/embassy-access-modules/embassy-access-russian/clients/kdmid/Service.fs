module EA.Russian.Clients.Kdmid.Service

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients
open EA.Core.Domain
open EA.Russian.Clients.Kdmid
open EA.Russian.Clients.Kdmid.Web
open EA.Russian.Clients.Domain.Kdmid

let private setInProcessState request =
    fun (client: Client) ->
        {
            request with
                ProcessState = InProcess
                Modified = DateTime.UtcNow
        }
        |> client.updateRequest

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

let private setAttempt request =
    let attemptLimit = 20
    let timeZone = request.Service.Embassy.TimeZone |> Option.defaultValue 0.

    let modified, attempt = request.Attempt
    let modified = modified.AddHours timeZone
    let today = DateTime.UtcNow.AddHours timeZone

    match modified.DayOfYear = today.DayOfYear, attempt > attemptLimit with
    | true, true ->
        Error
        <| Canceled $"Number of attempts reached the limit '%i{attemptLimit}' for today. The operation cancelled."
    | true, false ->
        {
            request with
                Attempt = DateTime.UtcNow, attempt + 1
        }
        |> Ok
    | _ ->
        {
            request with
                Attempt = DateTime.UtcNow, 1
        }
        |> Ok

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
                Modified = DateTime.UtcNow
        }

let private setFailedState error request =
    fun (updateRequest, restart, attempts) ->
        match error with
        | Operation {
                        Code = Some(Custom Web.Captcha.ERROR_CODE)
                    } ->
            match attempts <= 0 with
            | true ->
                "Limit of Captcha retries reached. The operation cancelled."
                |> Canceled
                |> Error
                |> async.Return
            | false -> request |> restart
        | _ ->
            {
                request with
                    ProcessState = Failed error
                    Modified = DateTime.UtcNow
            }
            |> setAttempt
            |> ResultAsync.wrap updateRequest
            |> ResultAsync.bind (fun _ ->
                $"{Environment.NewLine}%s{request.Service.Payload}" |> error.ExtendMsg |> Error)

let rec tryProcess (request: Request) =
    fun (client: Client) ->

        // define
        let setInProcessState r = client |> setInProcessState r

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

        let setAttempt =
            ResultAsync.bindAsync (fun (httpClient, r, qp, fd) ->
                r
                |> setAttempt
                |> ResultAsync.wrap client.updateRequest
                |> ResultAsync.map (fun r -> httpClient, r, qp, fd))

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

        let setProcessState =
            Async.bind (function
                | Ok r -> client.updateRequest |> setCompletedState r
                | Error error ->
                    let inline restart request = client |> tryProcess request
                    (client.updateRequest, restart, 3) |> setFailedState error request)

        // pipe
        let run =
            setInProcessState
            >> createPayload
            >> createHttpClient
            >> parseInitialPage
            >> setAttempt
            >> parseValidationPage
            >> parseAppointmentsPage
            >> parseConfirmationPage
            >> setProcessState

        request |> run

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

        let rec innerLoop (errors: Error' list) requests =
            async {
                match requests with
                | [] -> return Error errors
                | request :: requestsTail ->
                    match! client |> tryProcess request with
                    | Error error ->

                        do!
                            match request |> Notification.tryCreateFail error errorFilter with
                            | None -> () |> async.Return
                            | Some notification -> notification |> notify

                        return! requestsTail |> innerLoop (errors @ [ error ])

                    | Ok result ->

                        do!
                            match result |> Notification.tryCreate errorFilter with
                            | None -> () |> async.Return
                            | Some notification -> notification |> notify

                        return result |> Ok
            }

        requests |> List.ofSeq |> innerLoop []
