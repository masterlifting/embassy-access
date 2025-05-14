[<RequireQualifiedAccess>]
module internal EA.Italian.Services.Prenotami.Html

open System
open System.Text.RegularExpressions
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain
open EA.Core.Domain
open EA.Italian.Services.Router
open EA.Italian.Services.Domain.Prenotami

let initBrowser client =
    client.Browser.init { Name = "Crominium" }

let loadPage client =
    ResultAsync.bindAsync (client.Browser.loadPage ("https://prenotami.esteri.it" |> Uri))

let setLogin (request: Request<Payload>) client =
    ResultAsync.bindAsync (
        client.Browser.fillInput (Browser.Selector "//input[@id='login-email']") request.Payload.Credentials.Login
    )

let setPassword (request: Request<Payload>) client =
    ResultAsync.bindAsync (
        client.Browser.fillInput (Browser.Selector "//input[@id='login-password']") request.Payload.Credentials.Password
    )

let mouseShuffle client =
    ResultAsync.bindAsync client.Browser.mouseShuffle

let submitForm client =
    ResultAsync.bindAsync (
        client.Browser.submitForm
            (Browser.Selector "form#login-form")
            (Regex("https?://prenotami\\.esteri\\.it/(UserArea(\\?.*)?|Home/Login)$"))
    )

let clickBookTab client =
    let goToServices page =
        page
        |> client.Browser.mouseClick
            (Browser.Selector "//a[@id='advanced']")
            (Some(Regex("https?://prenotami\\.esteri\\.it/Services(\\?.*)?$")))

    ResultAsync.bindAsync (fun page ->
        page
        |> client.Browser.tryFindText (Browser.Selector "//title")
        |> ResultAsync.bindAsync (function
            | Some title ->
                match title.Contains "Unavailable" with
                | true ->
                    "The service is temporarily unavailable. Please try again later."
                    |> NotFound
                    |> Error
                    |> async.Return
                | false -> goToServices page
            | None -> goToServices page))

let chooseBookService (request: Request<Payload>) client =
    ResultAsync.bindAsync (fun page ->
        match request.Service.Id |> parse with
        | Ok(Visa service) ->
            match service with
            | Visa.Tourism1 _ -> "//a[@href='/Services/Booking/1151']" |> Ok
            | Visa.Tourism2 _ -> "//a[@href='/Services/Booking/1558']" |> Ok
        | Error _ ->
            $"The service Id '{request.Service.Id}' is not recognized to process prenotami."
            |> NotFound
            |> Error
        |> Result.map Browser.Selector
        |> ResultAsync.wrap (fun selector -> page |> client.Browser.mouseClick selector None))

let setResult (request: Request<Payload>) client =
    ResultAsync.bindAsync (fun page ->
        page
        |> client.Browser.tryFindText (Browser.Selector "//div[starts-with(@id, 'jconfirm-box')]//div")
        |> ResultAsync.bind (function
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
        |> Async.bind (fun r ->
            page
            |> client.Browser.closePage
            |> Async.map (function
                | Ok _ -> r
                | Error error -> Error error)))
