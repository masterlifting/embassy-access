[<RequireQualifiedAccess>]
module internal EA.Embassies.Russian.Kdmid.Order

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Embassies.Russian.Kdmid.Web
open EA.Embassies.Russian.Kdmid.Domain
open EA.Embassies.Russian.Kdmid.Dependencies

let private validateCity (request: Request) (payload: Payload) =

    match request.Service.Embassy.Id = payload.EmbassyId with
    | true -> Ok payload
    | false ->
        Error
        <| NotSupported
            $"The subdomain '{payload.SubDomain}' of the requested embassy '{request.Service.Embassy.ShortName}'"

let private parsePayload =
    ResultAsync.bind (fun (request: Request) ->
        request.Service.Payload
        |> Uri
        |> Payload.create
        |> Result.bind (validateCity request)
        |> Result.map (fun payload -> payload, request))

let private setInProcessState (deps: Order.Dependencies) request =
    deps.updateRequest {
        request with
            ProcessState = InProcess
            Modified = DateTime.UtcNow
    }

let private setAttemptCore (request: Request) =
    let attemptLimit = 20
    let timeZone = request.Service.Embassy.TimeZone |> Option.defaultValue 0.

    let modified, attempt = request.Attempt
    let modified = modified.AddHours timeZone
    let today = DateTime.UtcNow.AddHours timeZone

    match modified.DayOfYear = today.DayOfYear, attempt > attemptLimit with
    | true, true ->
        Error
        <| Canceled $"Number of attempts reached the limit '%i{attemptLimit}' for today. The operation"
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

let private setAttempt (deps: Order.Dependencies) =
    ResultAsync.bindAsync (fun (httpClient, queryParams, formData, request) ->
        request
        |> setAttemptCore
        |> ResultAsync.wrap deps.updateRequest
        |> ResultAsync.map (fun request -> httpClient, queryParams, formData, request))

let private setCompletedState (deps: Order.Dependencies) request =
    let message =
        match request.Appointments.IsEmpty with
        | true -> "No appointments found"
        | false ->
            match request.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
            | [] -> $"Found appointments: %i{request.Appointments.Count}"
            | confirmations -> $"Found confirmations: %i{confirmations.Length}"

    deps.updateRequest {
        request with
            ProcessState = Completed message
            Modified = DateTime.UtcNow
    }

let private handleFailedState error (deps: Order.Dependencies) restart request =
    match error with
    | Operation {
                    Code = Some(Custom Web.Captcha.ERROR_CODE)
                } ->
        match deps.RestartAttempts <= 0 with
        | true ->
            "Limit of restarting request due to captcha error reached. The request"
            |> Canceled
            |> Error
            |> async.Return
        | false ->
            {
                deps with
                    RestartAttempts = deps.RestartAttempts - 1
            }
            |> restart request
    | _ ->
        {
            request with
                ProcessState = Failed error
                Modified = DateTime.UtcNow
        }
        |> setAttemptCore
        |> ResultAsync.wrap deps.updateRequest
        |> ResultAsync.bind (fun _ -> Error <| error.extendMsg $"{Environment.NewLine}%s{request.Service.Payload}")

let private setProcessedState deps request restart confirmation =
    async {
        match! confirmation with
        | Error error -> return! request |> handleFailedState error deps restart
        | Ok request -> return! request |> setCompletedState deps
    }

let rec start (request: Request) =
    fun (deps: Order.Dependencies) ->

        // define
        let setInProcessState = setInProcessState deps
        let parsePayload = parsePayload
        let createHttpClient = Http.createClient
        let processInitialPage = InitialPage.handle deps
        let setAttempt = setAttempt deps
        let processValidationPage = ValidationPage.handle deps
        let processAppointmentsPage = AppointmentsPage.handle deps
        let processConfirmationPage = ConfirmationPage.handle deps
        let setProcessedState = setProcessedState deps request start

        // pipe
        let run =
            setInProcessState
            >> parsePayload
            >> createHttpClient
            >> processInitialPage
            >> setAttempt
            >> processValidationPage
            >> processAppointmentsPage
            >> processConfirmationPage
            >> setProcessedState

        request |> run

let pick (requests: Request seq) notify =
    fun (deps: Order.Dependencies) ->

        let inline errorFilter error =
            match error with
            | Operation reason ->
                match reason.Code with
                | Some(Custom Constants.ErrorCode.CONFIRMATION_EXISTS)
                | Some(Custom Constants.ErrorCode.NOT_CONFIRMED)
                | Some(Custom Constants.ErrorCode.REQUEST_DELETED) -> true
                | _ -> false
            | _ -> false

        let rec innerLoop (errors: Error' list) requests =
            async {
                match requests with
                | [] -> return Error errors
                | request :: requestsTail ->
                    match! deps |> start request with
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
