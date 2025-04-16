module EA.Italian.Services.Prenotami.Service

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Italian.Services.Prenotami.Web
open EA.Italian.Services.Domain.Prenotami

let private validateLimits (request: Request'<Request>) =
    request
    |> Request'<Request>.validateLimits
    |> Result.mapError (fun error -> $"{error} The operation cancelled." |> Canceled)

let private setFinalProcessState (request: Request'<Request>) requestPipe =
    fun updateRequest ->
        requestPipe
        |> Async.bind (function
            | Ok r ->
                Request'<Request>.updateLimits {
                    r with
                        Modified = DateTime.UtcNow
                        ProcessState = r.Payload |> Request.print |> Completed
                }
                |> updateRequest
            | Error error ->
                match error with
                | Operation reason ->
                    match reason.Code with
                    | Some(Custom Constants.ErrorCode.INITIAL_PAGE_ERROR) -> request
                    | _ -> request |> Request'<Request>.updateLimits
                | _ -> request |> Request'<Request>.updateLimits
                |> fun (r: Request'<Request>) ->
                    updateRequest {
                        r with
                            ProcessState = Failed error
                            Modified = DateTime.UtcNow
                    })

let tryProcess (request: Request'<Request>) =
    fun (client: Client) ->
        // define
        let setInitialProcessState =
            ResultAsync.wrap (fun (r: Request'<Request>) ->
                {
                    r with
                        ProcessState = InProcess
                        Modified = DateTime.UtcNow
                }
                |> client.updateRequest)

        let createHttpClient =
            ResultAsync.bind (fun (r: Request'<Request>) ->
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

let tryProcessFirst (requests: Request'<Request> seq) =
    fun (client: Client, notify) ->

        let rec processNextRequest (errors: Error' list) (remainingRequests: Request'<Request> list) =
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
