module EA.Italian.Services.Prenotami.Web.Html

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients
open Web.Clients.Domain
open EA.Core.Domain
open EA.Italian.Services.Domain.Prenotami
open EA.Italian.Services.Prenotami.Client

type internal Dependencies = {
    getInitialPage: unit -> Async<Result<Http.Response<string>, Error'>>
    setSessionCookie: Http.Response<string> -> Result<Http.Response<string>, Error'>
    solveCaptcha: string -> Async<Result<string, Error'>>
    buildFormData: string -> Map<string, string>
    postLoginPage: Map<string, string> -> Async<Result<Http.Response<string>, Error'>>
    setAuthCookie: Http.Response<string> -> Result<unit, Error'>
    getServicePage: unit -> Async<Result<Http.Response<string>, Error'>>
} with

    static member create credentials serviceId =
        fun (client: HttpClient) ->
            client.initClient ()
            |> Result.map (fun httpClient -> {
                getInitialPage = fun () -> httpClient |> client.getInitialPage
                setSessionCookie = fun response -> httpClient |> client.setSessionCookie response
                solveCaptcha =
                    fun siteKey ->
                        match httpClient.BaseAddress with
                        | null -> "Base address of HTTP client is not set." |> NotFound |> Error |> async.Return
                        | siteUri -> client.solveCaptcha siteUri siteKey
                buildFormData = client.buildFormData credentials
                postLoginPage = fun formData -> httpClient |> client.postLoginPage formData
                setAuthCookie = fun response -> httpClient |> client.setAuthCookie response
                getServicePage = fun () -> httpClient |> client.getServicePage serviceId
            })

let private getCaptchaSiteKey page =
    Html.load page
    |> Result.bind (Html.getNodes "//script[contains(@src, 'recaptcha/enterprise.js')]")
    |> Result.toOption
    |> Option.bind id
    |> Option.bind (fun nodes ->
        nodes
        |> Seq.tryPick (fun node ->
            node
            |> Html.getAttributeValue "src"
            |> Result.toOption
            |> Option.bind id
            |> Option.bind (fun src ->
                if src.Contains "render=" then
                    let startIdx = src.IndexOf("render=") + 7
                    let endIdx = src.IndexOf("&", startIdx)
                    let value =
                        if endIdx > 0 then
                            src.Substring(startIdx, endIdx - startIdx)
                        else
                            src.Substring(startIdx)
                    Some value
                else
                    None)))
    |> function
        | Some siteKey -> Ok siteKey
        | None -> Error <| NotFound "Recaptcha siteKey of 'Prenotami Initial Page' not found."

let private getServiceState page =
    Html.load page
    |> Result.bind (Html.getNodes "//div[@id='serviceState']")
    |> Result.toOption
    |> Option.bind id
    |> Option.bind (fun nodes ->
        nodes
        |> Seq.tryPick (fun node ->
            node
            |> Html.getAttributeValue "data-service-state"
            |> Result.toOption
            |> Option.bind id))
    |> function
        | Some state -> Ok state
        | None -> Error <| NotFound "Service state of 'Prenotami Initial Page' not found."

let internal processWeb =
    fun (deps: Dependencies) ->
        deps.getInitialPage ()
        |> ResultAsync.bind deps.setSessionCookie
        |> ResultAsync.bind (fun response -> getCaptchaSiteKey response.Content)
        |> ResultAsync.bindAsync deps.solveCaptcha
        |> ResultAsync.map deps.buildFormData
        |> ResultAsync.bindAsync deps.postLoginPage
        |> ResultAsync.bind deps.setAuthCookie
        |> ResultAsync.bindAsync deps.getServicePage
        |> ResultAsync.bind (fun response -> getServiceState response.Content)

let internal setRequestState (request: Request<Payload>) =
    fun (state: string) ->
            request
