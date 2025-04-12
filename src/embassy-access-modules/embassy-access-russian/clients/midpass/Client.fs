module EA.Russian.Clients.Midpass.Client

open Web.Clients
open Web.Clients.Domain
open EA.Russian.Clients.Domain.Midpass

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
