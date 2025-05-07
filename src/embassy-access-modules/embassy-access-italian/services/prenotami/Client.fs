module EA.Italian.Services.Prenotami.Client

open System.Threading
open Web.Clients
open EA.Core.DataAccess
open EA.Italian.Services.Domain.Prenotami
open EA.Italian.Services.DataAccess.Prenotami

type Dependencies = {
    ct: CancellationToken
    RequestStorage: Request.Storage<Payload, Payload.Entity>
}

let init (deps: Dependencies) =
    {
        updateRequest = fun request -> deps.RequestStorage |> Storage.Request.Command.createOrUpdate request
        initBrowserProvider = fun () -> Browser.Provider.init { Host = "https://prenotami.esteri.it" }
        loadBrowserPage = fun pageUri provider -> provider |> Browser.Page.load pageUri
        fillBrowserForm = fun selector value page -> page |> Browser.Page.Form.fill selector value
        clickBrowserButton = fun selector page -> page |> Browser.Page.Button.click selector
    }
    |> Ok
