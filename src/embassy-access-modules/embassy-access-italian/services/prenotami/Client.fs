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
    RequestStorage: Request.Storage<Payload, Payload.Entity>
}

type PersistenceClient = {
    updateRequest: Request<Payload> -> Async<Result<Request<Payload>, Error'>>
}
type HttpClient = {
    initClient: unit -> Result<Http.Client, Error'>
    getInitialPage: Http.Client -> Async<Result<Http.Response<string>, Error'>>
    setSessionCookie: Http.Response<string> -> Http.Client -> Result<Http.Response<string>, Error'>
    solveCaptcha: Uri -> string -> Async<Result<string, Error'>>
    buildFormData: Credentials -> string -> Map<string, string>
    postLoginPage: Map<string, string> -> Http.Client -> Async<Result<Http.Response<string>, Error'>>
    setAuthCookie: Http.Response<string> -> Http.Client -> Result<unit, Error'>
    getServicePage: ServiceId -> Http.Client -> Async<Result<Http.Response<string>, Error'>>
}
type Client = {
    Persistence: PersistenceClient
    Http: HttpClient
}

let init (deps: Dependencies) = {
    Persistence = {
        updateRequest = fun request -> deps.RequestStorage |> Storage.Request.Command.createOrUpdate request
    }
    Http = {
        initClient =
            fun () ->
                Http.Client.init {
                    BaseUrl = "https://prenotami.esteri.it"
                    Headers = None
                }
        getInitialPage = fun client -> client |> Web.Http.getInitialPage deps.ct
        setSessionCookie = fun response client -> client |> Web.Http.setSessionCookie response
        solveCaptcha = Web.Http.solveCaptcha deps.ct
        buildFormData = Web.Http.buildFormData
        postLoginPage = fun formData client -> client |> Web.Http.postLoginPage deps.ct formData
        setAuthCookie = fun response client -> client |> Web.Http.setAuthCookie response
        getServicePage = fun serviceId client -> client |> Web.Http.getServicePage deps.ct serviceId
    }
}
