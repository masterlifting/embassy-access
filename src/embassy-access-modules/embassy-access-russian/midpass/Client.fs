module EA.Russian.Midpass.Client

open System.Threading
open Web.Clients
open Web.Clients.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Russian.Domain.Midpass
open EA.Russian.DataAccess.Midpass

type Dependencies = {
    ct: CancellationToken
    RequestStorage: Request.Storage<Payload, Payload.Entity>
}

let init (deps: Dependencies) =
    let initHttpClient (url: string) =
        {
            Http.BaseUrl = "midpass.ru"
            Http.Headers = None
        }
        |> Http.Client.init

    {
        initHttpClient = initHttpClient
        updateRequest = fun r -> async.Return(Ok r)
    }
    |> Ok
