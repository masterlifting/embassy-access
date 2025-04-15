module EA.Italian.Services.Prenotami.Service

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Italian.Services.Prenotami.Web
open EA.Italian.Services.Domain.Prenotami

let private createPayload request =
    request.Service.Payload |> Payload.create

let private validateLimits request =
    request
    |> Request.validateLimits
    |> Result.mapError (fun error -> $"{error} The operation cancelled." |> Canceled)

let private setFinalProcessState request requestPipe =
    fun updateRequest ->
        requestPipe
        |> Async.bind (function
            | Ok r ->
                Request.updateLimits {
                    r with
                        Modified = DateTime.UtcNow
                        ProcessState =
                            match r.Appointments.IsEmpty with
                            | true -> "No appointments found"
                            | false ->
                                match r.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
                                | [] -> $"Found appointments: %i{r.Appointments.Count}"
                                | confirmations -> $"Found confirmations: %i{confirmations.Length}"
                            |> Completed
                }
                |> updateRequest
            | Error error ->
                match error with
                | Operation reason ->
                    match reason.Code with
                    | Some(Custom Constants.ErrorCode.INITIAL_PAGE_ERROR) -> request
                    | _ -> request |> Request.updateLimits
                | _ -> request |> Request.updateLimits
                |> fun r ->
                    updateRequest {
                        r with
                            ProcessState = Failed error
                            Modified = DateTime.UtcNow
                    })

let tryProcess (request: Request) =
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

        let createPayload =
            ResultAsync.bind (fun r -> r |> createPayload |> Result.map (fun payload -> r, payload))

        let createHttpClient =
            ResultAsync.bind (fun (r, p) ->
                p |> client.initHttpClient |> Result.map (fun httpClient -> httpClient, r, p))

        let parseInitialPage =
            ResultAsync.bindAsync (fun (httpClient, r, p) ->
                (httpClient, client.getInitialPage)
                |> Html.InitialPage.parse ()
                |> ResultAsync.map (fun formData -> r)
                |> ResultAsync.mapError (fun error ->
                    Operation {
                        Message = error.Message
                        Code = Constants.ErrorCode.INITIAL_PAGE_ERROR |> Custom |> Some
                    }))

        let setFinalProcessState requestRes =
            client.updateRequest |> setFinalProcessState request requestRes

        // pipe
        request
        |> validateLimits
        |> setInitialProcessState
        |> createPayload
        |> createHttpClient
        |> parseInitialPage
        |> setFinalProcessState

let tryProcessFirst (requests: Request seq) =
    fun (client: Client, notify) ->

        let inline errorFilter _ = true

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
