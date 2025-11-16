module EA.Italian.Services.Prenotami.Client

open System
open System.Threading
open Infrastructure.Domain
open Web.Clients
open Web.Clients.Domain
open Web.Clients.Domain.BrowserWebApi
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
    init: unit -> Result<Http.Client, Error'>
    openTab: Dto.Open -> Http.Client -> Async<Result<string, Error'>>
    fillCredentials: string -> Dto.Fill -> Http.Client -> Async<Result<unit, Error'>>
    submitCaptcha: string -> Dto.Execute -> Http.Client -> Async<Result<unit, Error'>>
    submitCredentials: string -> Dto.Click -> Http.Client -> Async<Result<unit, Error'>>
    clickBookService: string -> Dto.Click -> Http.Client -> Async<Result<unit, Error'>>
    clickBookAppointment: string -> Dto.Click -> Http.Client -> Async<Result<unit, Error'>>
    extractResult: string -> Dto.Extract -> Http.Client -> Async<Result<string option, Error'>>
    closeTab: string -> Http.Client -> Async<Result<unit, Error'>>
}

type Client = {
    Persistence: Persistence
    BrowserWebApi: BrowserWebApi
}

let init (deps: Dependencies) = {
    Persistence = {
        updateRequest = fun request -> deps.RequestStorage |> Storage.Request.Command.upsert request
    }
    BrowserWebApi = {
        init = fun () -> BrowserWebApi.Client.init { BaseUrl = deps.BrowserWebApiUrl }
        openTab = fun dto client -> client |> BrowserWebApi.Request.Tab.open' dto deps.ct
        fillCredentials = fun tabId dto client -> client |> BrowserWebApi.Request.Tab.fill tabId dto deps.ct
        submitCaptcha = fun tabId dto client -> client |> BrowserWebApi.Request.Tab.Element.execute tabId dto deps.ct
        submitCredentials = fun tabId dto client -> client |> BrowserWebApi.Request.Tab.Element.click tabId dto deps.ct
        clickBookService = fun tabId dto client -> client |> BrowserWebApi.Request.Tab.Element.click tabId dto deps.ct
        clickBookAppointment =
            fun tabId dto client -> client |> BrowserWebApi.Request.Tab.Element.click tabId dto deps.ct
        extractResult = fun tabId dto client -> client |> BrowserWebApi.Request.Tab.Element.extract tabId dto deps.ct
        closeTab = fun tabId client -> client |> BrowserWebApi.Request.Tab.close tabId deps.ct
    }
}
