module EA.Italian.Services.Prenotami.Client

open Web.Clients
open Web.Clients.Domain
open EA.Italian.Services.Domain.Prenotami

let init (deps: Dependencies) =
    let initHttpClient (url: string) =
        {
            Http.Host = "midpass.ru"
            Http.Headers = None
        }
        |> Http.Provider.init

    {
        initHttpClient = initHttpClient
        updateRequest = fun r -> async.Return(Ok r)
    }
    |> Ok
