module EA.Italian.Services.Prenotami.Web.BrowserWebApi

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Italian.Services
open EA.Italian.Services.Router
open EA.Italian.Services.Domain.Prenotami
open EA.Italian.Services.Prenotami.Client
open Web.Clients.Domain.BrowserWebApi

let private getBookAppointmentSelector serviceId =
    match serviceId |> Router.parse with
    | Ok(Visa service) ->
        match service with
        | Visa.Tourism1 _ -> "a[href='/Services/Booking/1151']"
        | Visa.Tourism2 _ -> "a[href='/Services/Booking/1258']"
        |> Ok
    | Error _ ->
        $"The service Id '{serviceId}' is not recognized to process prenotami."
        |> NotFound
        |> Error

let private resultAsync = ResultAsyncBuilder()

let processWebSite credentials serviceId =
    fun (api: BrowserWebApi) ->
        resultAsync {
            let! httpClient = api.init () |> async.Return

            let! tabId =
                httpClient
                |> api.openTab {
                    Dto.Open.Url = "https://prenotami.esteri.it"
                }

            do!
                httpClient
                |> api.fillCredentials tabId {
                    Dto.Fill.Inputs = [
                        {
                            Dto.Input.Selector = "#login-email"
                            Value = credentials.Login
                        }
                        {
                            Dto.Input.Selector = "#login-password"
                            Value = credentials.Password
                        }
                    ]
                }

            do!
                httpClient
                |> api.submitCredentials tabId {
                    Dto.Execute.Selector = "form#login-form"
                    Dto.Execute.Function = "submit();"
                }

            do!
                httpClient
                |> api.clickBookService tabId {
                    Dto.Click.Selector = "a#advanced"
                }

            let! selector = getBookAppointmentSelector serviceId |> async.Return
            do! httpClient |> api.clickBookAppointment tabId { Dto.Click.Selector = selector }

            let! result =
                httpClient
                |> api.extractResult tabId {
                    Dto.Extract.Selector = "div.jconfirm-box div.jconfirm-content"
                }
                |> ResultAsync.bind (function
                    | Some text -> text |> Ok
                    | None ->
                        "Could not extract the result from the page."
                        |> NotFound
                        |> Error)

            do! httpClient |> api.closeTab tabId

            return result |> Ok |> async.Return
        }
