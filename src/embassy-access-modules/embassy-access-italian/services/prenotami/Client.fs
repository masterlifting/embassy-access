module EA.Italian.Services.Prenotami.Client

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
