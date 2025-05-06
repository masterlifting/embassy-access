module EA.Italian.Services.Prenotami.Client

open System.Threading
open Web.Clients
open Web.Clients.Domain
open EA.Core.DataAccess
open EA.Italian.Services.Domain.Prenotami
open EA.Italian.Services.DataAccess.Prenotami

type Dependencies = {
    ct: CancellationToken
    RequestStorage: Request.Storage<Payload, Payload.Entity>
}

let init (deps: Dependencies) =
    let initHttpClient () =
        let host = "https://prenotami.esteri.it"
        
        {
            Http.Host = host
            Http.Headers =
                Map [
                    "Host", [ "prenotami.esteri.it" ]
                    "Accept",
                    [
                        "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7"
                    ]
                    "Accept-Encoding", [ "gzip"; "deflate"; "br"; "zstd" ]
                    "Accept-Language", [ "en,ru;q=0.9,en-US;q=0.8" ]
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
        |>Http.Provider.init

    {
        initHttpClient = initHttpClient
        updateRequest = fun request -> deps.RequestStorage |> Storage.Request.Command.createOrUpdate request
        getInitialPage =
            fun request httpClient ->
                httpClient
                |> Http.Request.get request deps.ct
                |> Http.Response.String.read deps.ct
    }
    |> Ok
