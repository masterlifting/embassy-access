module EA.Italian.Services.Prenotami.Service

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Italian.Services.Domain.Prenotami

let private validateLimits (request: Request<Payload>) =
    request.ValidateLimits()
    |> Result.mapError (fun error -> $"{error} The operation cancelled." |> Canceled)

let private processWebSite request client =
    Html.loadPage client
    |> Html.setLogin request client
    |> Html.setPassword request client
    |> Html.mouseShuffle client
    |> Html.submitForm client
    |> Html.clickBookTab client
    |> Html.chooseBookService request client
    |> Html.setResult request client

let private setFinalProcessState (request: Request<Payload>) requestPipe =
    fun updateRequest ->
        requestPipe
        |> Async.bind (function
            | Ok(r: Request<Payload>) ->
                updateRequest {
                    r.UpdateLimits() with
                        Modified = DateTime.UtcNow
                        ProcessState = r.Payload |> Payload.print |> Completed
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
                    })

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

        let setFinalState requestRes =
            client.updateRequest |> setFinalProcessState request requestRes

        // pipe
        request |> validateLimits |> setInitialState |> processWebSite |> setFinalState

let tryProcessFirst requests =
    fun (client: Client, notify) ->

        let rec processNextRequest (remainingRequests: Request<Payload> list) =
            async {
                match remainingRequests with
                | [] ->
                    return
                        "All of the attempts to get the first available result have been reached. The Operation canceled."
                        |> Canceled
                        |> Error
                | request :: requestsTail ->
                    match! client |> tryProcess request with
                    | Error error ->
                        do! error |> Error |> notify
                        return! requestsTail |> processNextRequest
                    | Ok result ->
                        do! result |> Ok |> notify
                        return Ok result
            }

        requests |> List.ofSeq |> processNextRequest
