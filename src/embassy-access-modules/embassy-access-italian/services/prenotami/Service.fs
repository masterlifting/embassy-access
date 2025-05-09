module EA.Italian.Services.Prenotami.Service

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain
open EA.Core.Domain
open EA.Italian.Services.Domain.Prenotami

let private validateLimits (request: Request<Payload>) =
    request.ValidateLimits()
    |> Result.mapError (fun error -> $"{error} The operation cancelled." |> Canceled)

let private processWebSite (request: Request<Payload>) (client: Client) =
    //define

    let setLogin () =
        client.Browser.fillInput (Browser.Selector "//input[@id='login-email']") request.Payload.Credentials.Login

    let setPassword =
        ResultAsync.bindAsync (fun _ ->
            client.Browser.fillInput
                (Browser.Selector "//input[@id='login-password']")
                request.Payload.Credentials.Password)

    let mouseShuffle = ResultAsync.bindAsync client.Browser.mouseShuffle

    let submitForm =
        ResultAsync.bindAsync (fun _ ->
            client.Browser.executeCommand (Browser.Selector "form#login-form") "form => form.submit()")

    let clickBookTab =
        ResultAsync.bindAsync (fun _ -> client.Browser.clickButton (Browser.Selector "//a[@id='advanced']"))

    let chooseBookService =
        ResultAsync.bindAsync (fun _ ->
            match request.Service.Id.Value |> Graph.NodeId.splitValues with
            | [ _; _; _; "0"; _ ] -> "//a[@href='/Services/Booking/1151']" |> Ok //Tourism 1
            | [ _; _; _; "1"; _ ] -> "//a[@href='/Services/Booking/1558']" |> Ok //Tourism 2
            | _ ->
                $"The service Id '{request.Service.Id}' is not recognized to process prenotami."
                |> NotFound
                |> Error
            |> Result.map Browser.Selector
            |> ResultAsync.wrap client.Browser.clickButton)

    let setResult =
        ResultAsync.bindAsync (fun _ ->
            client.Browser.tryFindText (Browser.Selector "//div[starts-with(@id, 'jconfirm-box')]//div"))
        >> ResultAsync.bind (function
            | Some text ->
                Ok {
                    request with
                        Payload = {
                            request.Payload with
                                State =
                                    match text.Contains "Please check again" with
                                    | true -> NoAppointments
                                    | false ->
                                        text
                                        |> Appointment.parse
                                        |> Result.map HasAppointments
                                        |> Result.defaultValue NoAppointments
                        }
                }
            | None ->
                "The service is not available at the moment. Please try again later."
                |> NotFound
                |> Error)

    // pipe
    setLogin ()
    |> setPassword
    |> mouseShuffle
    |> submitForm
    |> clickBookTab
    |> chooseBookService
    |> setResult

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

        let processWebSite =
            ResultAsync.bindAsync (fun r -> client |> processWebSite r |> ResultAsync.map (fun _ -> r))

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
