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
    let nodeSubName = [ payload.Country; payload.City ] |> Graph.buildNodeNameOfSeq

    match request.Service.Embassy.Name.IndexOf(nodeSubName) <> -1 with
    | true -> Ok payload
    | false ->
        Error
        <| NotSupported $"Requested {payload.City} for the subdomain {payload.SubDomain}"

let private parsePayload =
    ResultAsync.bind (fun request ->
        request.Service.Payload
        |> Uri
        |> Payload.create
        |> Result.bind (validateCity request)
        |> Result.map (fun payload -> payload, request))

let private setInProcessState (deps: Order.Dependencies) request =
    deps.updateRequest
        { request with
            ProcessState = InProcess
            Modified = DateTime.UtcNow }

let private setAttemptCore timeZone request =
    let modified, attempt = request.Attempt
    let modified = modified.AddHours timeZone
    let today = DateTime.UtcNow.AddHours timeZone

    match modified.DayOfYear = today.DayOfYear, attempt > 20 with
    | true, true ->
        Error
        <| Canceled $"The request was cancelled due to the number of attempts reached the %i{attempt}."
    | true, false ->
        Ok
        <| { request with
               Attempt = DateTime.UtcNow, attempt + 1 }
    | _ ->
        Ok
        <| { request with
               Attempt = DateTime.UtcNow, 1 }

let private setAttempt timeZone (deps: Order.Dependencies) =
    ResultAsync.bindAsync (fun (httpClient, queryParams, formData, request) ->
        request
        |> setAttemptCore timeZone
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

    deps.updateRequest
        { request with
            ProcessState = Completed message
            Modified = DateTime.UtcNow }

let private setFailedState error (deps: Order.Dependencies) request =
    let attempt =
        match error with
        | Operation { Code = Some(Custom Web.Captcha.ErrorCode) } -> request.Attempt
        | _ -> DateTime.UtcNow, snd request.Attempt + 1

    deps.updateRequest
        { request with
            ProcessState = Failed error
            Attempt = attempt
            Modified = DateTime.UtcNow }
    |> ResultAsync.bind (fun _ -> Error <| error.add $"Payload: %s{request.Service.Payload}")

let private setProcessedState deps request confirmation =
    async {
        match! confirmation with
        | Error error -> return! request |> setFailedState error deps
        | Ok request -> return! request |> setCompletedState deps
    }

let start order =
    fun (deps: Order.Dependencies) ->

        // define
        let setInProcessState = setInProcessState deps
        let parsePayload = parsePayload
        let createHttpClient = Http.createClient
        let processInitialPage = InitialPage.handle deps
        let setAttempt = setAttempt order.TimeZone deps
        let processValidationPage = ValidationPage.handle deps
        let processAppointmentsPage = AppointmentsPage.handle deps
        let processConfirmationPage = ConfirmationPage.handle deps
        let setProcessedState = setProcessedState deps order.Request

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

        order.Request |> run

let pick order =
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

        let rec innerLoop (errors: Error' list) startOrders =
            async {
                match startOrders with
                | [] ->
                    return
                        match errors.Length with
                        | 0 -> Error [ "Orders to handle" |> NotFound ]
                        | _ -> Error errors
                | startOrder :: startOrdersTail ->
                    match! deps |> start startOrder with
                    | Error error ->

                        do!
                            match error |> Notification.tryCreateFail startOrder.Request.Id errorFilter with
                            | None -> () |> async.Return
                            | Some notification -> notification |> order.notify

                        return! startOrdersTail |> innerLoop (errors @ [ error ])

                    | Ok result ->

                        do!
                            match result |> Notification.tryCreate errorFilter with
                            | None -> () |> async.Return
                            | Some notification -> notification |> order.notify

                        return result |> Ok
            }

        order.StartOrders |> List.ofSeq |> innerLoop []
