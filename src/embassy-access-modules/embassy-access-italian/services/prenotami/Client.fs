module EA.Italian.Services.Prenotami.Client

open Web.Clients
open Web.Clients.Domain
open EA.Core.DataAccess
open EA.Italian.Services.Domain.Prenotami

let init (deps: Dependencies) =
    let initHttpClient (credentials: Credentials) =
        let host = "https://prenotami.esteri.it"
        {
            Http.Host = host
            Http.Headers =
                Map [
                    "Origin", [ host ]
                    "Accept",
                    [
                        "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/png,image/svg+xml,*/*;q=0.8"
                    ]
                    "Accept-Language", [ "en-US,en;q=0.8" ]
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
        }
        |> Http.Provider.init

    {
        initHttpClient = initHttpClient
        updateRequest = fun request -> deps.RequestsTable |> Storage.Request.Command.update request
        getInitialPage =
            fun request httpClient ->
                httpClient
                |> Http.Request.get request deps.CancellationToken
                |> Http.Response.String.read deps.CancellationToken
    }
    |> Ok
