module EA.Italian.Services.Prenotami.Client

open System
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
        Browser = {|
            initProvider = fun () -> Browser.Provider.init { Host = "https://prenotami.esteri.it" }
            loadPage = fun pageUri provider -> provider |> Browser.Page.load pageUri
            fillInput = fun selector value page -> page |> Browser.Page.Input.fill selector value
            clickButton = fun selector page -> page |> Browser.Page.Button.click selector
            mouseShuffle = fun page -> page |> Browser.Page.Mouse.shuffle (TimeSpan.FromSeconds 20.0)
            executeCommand = fun selector command page -> page |> Browser.Page.Html.execute selector command
        |}
    }
    |> Ok
