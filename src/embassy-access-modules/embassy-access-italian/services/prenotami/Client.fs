module EA.Italian.Services.Prenotami.Client

open System
open System.Threading
open Infrastructure.Domain
open Web.Clients
open Web.Clients.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Italian.Services.Domain.Prenotami
open EA.Italian.Services.DataAccess.Prenotami

type Dependencies = {
    ct: CancellationToken
    BrowserWebApiUrl: string
    RequestStorage: Request.Storage<Payload, Payload.Entity>
}

type Persistence = {
    updateRequest: Request<Payload> -> Async<Result<Request<Payload>, Error'>>
}

type BrowserWebApi = {
    initClient: unit -> Result<Http.Client, Error'>
    openTab: BrowserWebApi.Dto.Open -> Http.Client -> Async<Result<string, Error'>>
    fillCredentials: string -> BrowserWebApi.Dto.Fill -> Http.Client -> Async<Result<unit, Error'>>
    submitCredentials: string -> BrowserWebApi.Dto.Execute -> Http.Client -> Async<Result<unit, Error'>>
    clickServices: string -> BrowserWebApi.Dto.Click -> Http.Client -> Async<Result<unit, Error'>>
    clickVisa: string -> BrowserWebApi.Dto.Click -> Http.Client -> Async<Result<unit, Error'>>
    closeTab: string -> Http.Client -> Async<Result<unit, Error'>>
}

type Client = {
    Persistence: Persistence
    BrowserWebApi: BrowserWebApi
}

let init (deps: Dependencies) = {
    Persistence = {
        updateRequest = fun request -> deps.RequestStorage |> Storage.Request.Command.createOrUpdate request
    }
    BrowserWebApi = {
        initClient = fun () -> BrowserWebApi.Client.init { BaseUrl = deps.BrowserWebApiUrl }
        openTab = fun dto client -> client |> BrowserWebApi.Request.Tab.open' dto deps.ct
        fillCredentials = fun tabId dto client -> client |> BrowserWebApi.Request.Tab.fill tabId dto deps.ct
        submitCredentials =
            fun tabId dto client -> client |> BrowserWebApi.Request.Tab.Element.execute tabId dto deps.ct
        clickServices = fun tabId dto client -> client |> BrowserWebApi.Request.Tab.Element.click tabId dto deps.ct
        clickVisa = fun tabId dto client -> client |> BrowserWebApi.Request.Tab.Element.click tabId dto deps.ct
        closeTab = fun tabId client -> client |> BrowserWebApi.Request.Tab.close tabId deps.ct
    }
}
