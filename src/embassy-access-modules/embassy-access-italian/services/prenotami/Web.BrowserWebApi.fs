module EA.Italian.Services.Prenotami.Web.BrowserWebApi

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Http
open Web.Clients.Domain.Http
open EA.Italian.Services
open EA.Italian.Services.Router
open EA.Italian.Services.Domain.Prenotami
open Web.Clients.Domain.BrowserWebApi

let private createOpenDto () = {
    Dto.Open.Url = "https://prenotami.esteri.it"
}

let private createFillCredentialsDto credentials = {
    Dto.Fill.Inputs = [
        {
            Dto.Input.Selector = "#Email"
            Value = credentials.Login
        }
        {
            Dto.Input.Selector = "#Password"
            Value = credentials.Password
        }
    ]
}

let private createSubmitCredentialsDto () = {
    Dto.Execute.Selector = "#FormLogin"
    Dto.Execute.Function = "submit();"
}

let private createClickServicesDto () = {
    Dto.Click.Selector = "a[href='/Services']"
}

let private createClickVisaDto serviceId =
    match serviceId |> Router.parse with
    | Ok(Visa service) ->
        match service with
        | Visa.Tourism1 _ ->
            {
                Dto.Click.Selector = "a[href='/Services/Booking/1151']"
            }
            |> Ok
        | Visa.Tourism2 _ ->
            {
                Dto.Click.Selector = "a[href='/Services/Booking/1258']"
            }
            |> Ok
    | Error _ ->
        $"The service Id '{serviceId}' is not recognized to process prenotami."
        |> NotFound
        |> Error

let private resultAsync = ResultAsyncBuilder()

let processWebSite credentials serviceId =
    fun (api: Prenotami.Client.BrowserWebApi) ->
        resultAsync {
            let! httpClient = api.initClient () |> async.Return
            let! tabId = httpClient |> api.openTab (createOpenDto ())
            do! httpClient |> api.fillCredentials tabId (createFillCredentialsDto credentials)
            do! httpClient |> api.submitCredentials tabId (createSubmitCredentialsDto ())
            do! httpClient |> api.clickServices tabId (createClickServicesDto ())
            let! clickVisaDto = createClickVisaDto serviceId |> async.Return
            do! httpClient |> api.clickVisa tabId clickVisaDto
            return httpClient |> api.closeTab tabId
        }
