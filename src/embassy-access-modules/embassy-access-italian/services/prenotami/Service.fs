﻿module EA.Italian.Services.Prenotami.Service

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Italian.Services.Prenotami.Web
open EA.Italian.Services.Domain.Prenotami

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
                        ProcessState = r.Payload |> Payload.print |> Completed
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
                r.Payload.Credentials
                |> client.initHttpClient
                |> Result.map (fun httpClient -> httpClient, r))

        let parseInitialPage =
            ResultAsync.bindAsync (fun (httpClient, r) ->
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
        |> createHttpClient
        |> parseInitialPage
        |> setFinalProcessState

let tryProcessFirst (requests: Request<Payload> seq) =
    fun (client: Client, notify) ->

        let rec processNextRequest (errors: Error' list) (remainingRequests: Request<Payload> list) =
            async {
                match remainingRequests with
                | [] -> return Error(List.rev errors)
                | request :: requestsTail ->
                    match! client |> tryProcess request with
                    | Error error ->
                        do!
                            match request.Payload |> Payload.printError error with
                            | Some msg -> msg |> notify
                            | None -> async.Return()

                        return! requestsTail |> processNextRequest (error :: errors)

                    | Ok result ->
                        do! result.Payload |> Payload.print |> notify
                        return Ok result
            }

        processNextRequest [] (requests |> List.ofSeq)
