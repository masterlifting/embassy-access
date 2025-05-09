module EA.Italian.Services.Prenotami.Client

open System
open System.Threading
open Infrastructure.Prelude
open Web.Clients
open EA.Core.DataAccess
open EA.Italian.Services.Domain.Prenotami
open EA.Italian.Services.DataAccess.Prenotami

type Dependencies = {
    ct: CancellationToken
    RequestStorage: Request.Storage<Payload, Payload.Entity>
}

let init (deps: Dependencies) =
    Browser.Client.init {
        PageUri = "https://prenotami.esteri.it" |> Uri
    }
    |> ResultAsync.map (fun browserClient -> {
        updateRequest = fun request -> deps.RequestStorage |> Storage.Request.Command.createOrUpdate request
        Browser = {|
            fillInput = fun selector value -> browserClient |> Browser.Page.Input.fill selector value
            clickButton = fun selector -> browserClient |> Browser.Page.Button.click selector
            mouseShuffle = fun () -> browserClient |> Browser.Page.Mouse.shuffle (TimeSpan.FromSeconds 20.0)
            executeCommand = fun selector command -> browserClient |> Browser.Page.Html.execute selector command
            tryFindText = fun selector -> browserClient |> Browser.Page.Html.tryFindText selector
        |}
    })
