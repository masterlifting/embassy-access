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
    WebBrowser: Web.Clients.Domain.Browser.Client
}

let init (deps: Dependencies) = {
    updateRequest = fun request -> deps.RequestStorage |> Storage.Request.Command.createOrUpdate request
    Browser = {|
        loadPage = fun uri -> deps.WebBrowser |> Browser.Page.load uri
        closePage = fun page -> page |> Browser.Page.close
        fillInput = fun selector value page -> page |> Browser.Page.Input.fill selector value
        mouseClick = fun selector awaiter page -> page |> Browser.Page.Mouse.click selector awaiter
        tryFindText = fun selector page -> page |> Browser.Page.Text.tryFind selector
        submitForm = fun selector urlPattern page -> page |> Browser.Page.Form.submit selector urlPattern
    |}
}
