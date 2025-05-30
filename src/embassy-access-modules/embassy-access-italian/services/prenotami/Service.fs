﻿module EA.Italian.Services.Prenotami.Service

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Italian.Services.Domain.Prenotami

let private validate (request: Request<Payload>) =
    request.ValidateLimits()
    |> Result.mapError (fun error -> $"{error} Operation cancelled." |> Canceled)

let private processWebSite request client =
    Html.loadPage client
    |> Html.setLogin request client
    |> Html.setPassword request client
    |> Html.submitForm client
    |> Html.clickBookTab client
    |> Html.chooseBookService request client
    |> Html.setProcessResult request client

let private setFinalProcessState (request: Request<Payload>) =
    fun updateRequest ->
        Async.bind (function
            | Ok(r: Request<Payload>) ->
                updateRequest {
                    r.UpdateLimits() with
                        Modified = DateTime.UtcNow
                        ProcessState = r.Payload.State |> PayloadState.print |> Completed
                }
            | Error error ->
                match error with
                | Operation reason ->
                    match reason.Code with
                    | Some(Custom Constants.ErrorCode.TECHNICAL_ERROR) -> request
                    | _ -> request.UpdateLimits()
                | _ -> request.UpdateLimits()
                |> fun r ->
                    updateRequest {
                        r with
                            ProcessState = Failed error
                            Modified = DateTime.UtcNow
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

        let processWebSite = ResultAsync.bindAsync (fun r -> client |> processWebSite r)

        let setFinalState = client.updateRequest |> setFinalProcessState request

        // pipe
        request |> validate |> setInitialState |> processWebSite |> setFinalState

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
