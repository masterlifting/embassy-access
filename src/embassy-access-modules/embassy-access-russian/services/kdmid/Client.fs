module EA.Russian.Services.Kdmid.Client

open System
open System.Threading
open Web.Clients
open Web.Clients.Domain
open EA.Core.DataAccess
open EA.Russian.Services.Domain.Kdmid
open EA.Russian.Services.DataAccess.Kdmid

type Dependencies = {
    ct: CancellationToken
    RequestStorage: Request.Storage<Payload, Payload.Entity>
}

let init (deps: Dependencies) =
    let initHttpClient subdomain =
        let domain = $"%s{subdomain}.kdmid.ru"
        let host = $"https://%s{domain}"
        let headers =
            Map [
                "Host", [ domain ]
                "Origin", [ host ]
                "Accept",
                [
                    "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/png,image/svg+xml,*/*;q=0.8"
                ]
                "Accept-Language", [ "en-US,en;q=0.9,ru;q=0.8" ]
                "Connection", [ "keep-alive" ]
                "Sec-Fetch-Dest", [ "document" ]
                "Sec-Fetch-Mode", [ "navigate" ]
                "Sec-Fetch-Site", [ "same-origin" ]
                "Sec-Fetch-User", [ "?1" ]
                "Upgrade-Insecure-Requests", [ "1" ]
                "User-Agent",
                [
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:129.0) Gecko/20100101 Firefox/129.0"
                ]
            ]
            |> Some

        {
            Http.BaseUrl = host
            Http.Headers = headers
        }
        |> Http.Client.init

    {
        initHttpClient = initHttpClient
        updateRequest = fun request -> deps.RequestStorage |> Storage.Request.Command.createOrUpdate request
        getInitialPage =
            fun request httpClient ->
                httpClient
                |> Http.Request.get request deps.ct
                |> Http.Response.String.read deps.ct
        getCaptcha =
            fun request httpClient ->
                httpClient
                |> Http.Request.get request deps.ct
                |> Http.Response.Bytes.read deps.ct
        solveCaptcha = Web.AntiCaptcha.Number.fromImage deps.ct
        postValidationPage =
            fun request content httpClient ->
                httpClient
                |> Http.Request.post request content deps.ct
                |> Http.Response.String.readContent deps.ct
        postAppointmentsPage =
            fun request content httpClient ->
                httpClient
                |> Http.Request.post request content deps.ct
                |> Http.Response.String.readContent deps.ct
        postConfirmationPage =
            fun request content httpClient ->
                httpClient
                |> Http.Request.post request content deps.ct
                |> Http.Response.String.readContent deps.ct
    }
    |> Ok
