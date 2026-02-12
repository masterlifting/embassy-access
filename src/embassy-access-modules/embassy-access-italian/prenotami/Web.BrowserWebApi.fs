module EA.Italian.Prenotami.Web.BrowserWebApi

open System
open System.IO
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open EA.Italian
open EA.Italian.Router
open EA.Italian.Domain.Prenotami
open EA.Italian.Prenotami.Client
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

let private openHomePage api httpClient =
    httpClient
    |> api.openTab {
        Dto.Open.Url = "https://prenotami.esteri.it"
        Dto.Open.Expiration = 120UL
    }

let private fillCredentials tabId credentials api httpClient =
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

let private submitCaptcha tabId api httpClient =
    httpClient
    |> api.submitCaptcha tabId {
        Dto.Execute.Selector = None
        Dto.Execute.Function =
            """
                window['grecaptcha']['enterprise'].execute = 
                    function(sitekey, parameters) {
                        const action = parameters.action; 
                        const token = '';
                        return new Promise(r => r(token))
                    };
            """
    }

let private submitCredentials tabId api httpClient =
    httpClient
    |> api.submitCredentials tabId {
        Dto.Click.Selector = "button#captcha-trigger"
    }

let private clickBookService tabId api httpClient =
    httpClient |> api.clickBookService tabId { Dto.Click.Selector = "a#advanced" }

let private clickBookAppointment tabId serviceId api httpClient =
    getBookAppointmentSelector serviceId
    |> ResultAsync.wrap (fun selector -> httpClient |> api.clickBookAppointment tabId { Dto.Click.Selector = selector })

let private extractResult tabId api httpClient =
    httpClient
    |> api.extractResult tabId {
        Dto.Extract.Selector = "div.jconfirm-box div.jconfirm-content"
    }
    |> ResultAsync.bind (function
        | Some text -> text |> Ok
        | None -> "Could not extract the result from the page." |> NotFound |> Error)

let private closeTab tabId api httpClient = httpClient |> api.closeTab tabId

let processWebSite credentials serviceId =
    fun (api: BrowserWebApi) ->
        let wait s = Async.Sleep(s * 1000) |> Async.map Ok
        resultAsync {
            let! httpClient = api.init () |> async.Return
            let! tabId = httpClient |> openHomePage api
            let! result =
                resultAsync {
                    do! wait 2
                    do! httpClient |> fillCredentials tabId credentials api
                    do! httpClient |> submitCaptcha tabId api
                    do! wait 2
                    do! httpClient |> submitCredentials tabId api
                    do! wait 2
                    do! httpClient |> clickBookService tabId api
                    do! wait 2
                    do! httpClient |> clickBookAppointment tabId serviceId api
                    do! wait 2
                    return httpClient |> extractResult tabId api
                }
                |> ResultAsync.mapErrorAsync (fun error ->
                    async {
                        match! httpClient |> api.screenshot tabId with
                        | Ok screenshot ->
                            try
                                let debugDirectory = Path.Combine(Environment.CurrentDirectory, "debug")
                                debugDirectory |> Directory.CreateDirectory |> ignore

                                let timestamp = DateTime.UtcNow.ToString "dd_MM-HH_mm_ss"
                                let filePath = Path.Combine(debugDirectory, $"%s{timestamp}.png")

                                File.WriteAllBytes(filePath, screenshot)
                            with e ->
                                Log.crt $"Failed to write debug screenshot: %s{e.Message}"
                        | Error e -> Log.crt $"Failed to capture debug screenshot: %s{e.Message}"

                        return error
                    })
            do! httpClient |> closeTab tabId api
            return result |> Ok |> async.Return
        }
