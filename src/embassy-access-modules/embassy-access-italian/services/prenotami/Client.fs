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
            loadPage = fun uri -> browserClient |> Browser.Page.load uri
            closePage = fun () -> browserClient |> Browser.Page.close
            fillInput = fun selector value -> browserClient |> Browser.Page.Input.fill selector value
            mouseClick = fun selector pattern -> browserClient |> Browser.Page.Mouse.click selector pattern
            mouseShuffle = fun () -> browserClient |> Browser.Page.Mouse.shuffle (TimeSpan.FromSeconds 20.0)
            tryFindText = fun selector -> browserClient |> Browser.Page.Text.tryFind selector
            submitForm = fun selector urlPattern -> browserClient |> Browser.Page.Form.submit selector urlPattern
        |}
    })
