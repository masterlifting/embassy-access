module internal EA.Embassies.Russian.Kdmid.Request

open System
open Infrastructure
open EA.Core.Domain
open EA.Embassies.Russian.Kdmid.Domain

let validateCredentials (request: EA.Core.Domain.Request) credentials =
    match request.Service.Embassy.Country.City = credentials.City with
    | true -> Ok credentials
    | false ->
        Error
        <| NotSupported
            $"Embassy city '%A{request.Service.Embassy.Country.City}' is not matched with the requested City '%A{credentials.City}'."

let createCredentials =
    ResultAsync.bind (fun request ->
        request.Service.Payload
        |> createCredentials
        |> Result.bind (validateCredentials request)
        |> Result.map (fun credentials -> credentials, request))

let setInProcessState deps request =
    deps.updateRequest
        { request with
            ProcessState = InProcess
            Modified = DateTime.UtcNow }

let private setRequestAttempt timeShift request =
    let timeShift = timeShift |> int
    let modified, attempt = request.Attempt
    let modified = modified.AddHours timeShift
    let today = DateTime.UtcNow.AddHours timeShift

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

let setAttempt deps =
    ResultAsync.bindAsync (fun (httpClient, queryParams, formData, request) ->
        request
        |> setRequestAttempt deps.Configuration.TimeShift
        |> ResultAsync.wrap deps.updateRequest
        |> ResultAsync.map (fun request -> httpClient, queryParams, formData, request))

let private setCompletedState deps request =
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

let private setFailedState error deps request =
    let attempt =
        match error with
        | Operation { Code = Some Web.Captcha.CaptchaErrorCode } -> request.Attempt
        | _ -> DateTime.UtcNow, snd request.Attempt + 1

    deps.updateRequest
        { request with
            ProcessState = Failed error
            Attempt = attempt
            Modified = DateTime.UtcNow }
    |> ResultAsync.bind (fun _ -> Error <| error.add $"Payload: %s{request.Service.Payload}")

let completeConfirmation deps request confirmation =
    async {
        match! confirmation with
        | Error error -> return! request |> setFailedState error deps
        | Ok request -> return! request |> setCompletedState deps
    }
