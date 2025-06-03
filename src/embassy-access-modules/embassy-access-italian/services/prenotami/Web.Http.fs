module EA.Italian.Services.Prenotami.Web.Http

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Http
open Web.Clients.Domain.Http
open EA.Italian.Services
open EA.Italian.Services.Router
open EA.Italian.Services.Domain.Prenotami

let getInitialPage ct =
    fun httpClient ->
        httpClient
        |> Request.get { Path = "/"; Headers = None } ct
        |> Response.String.read ct

let setSessionCookie (response: Response<string>) =
    fun httpClient ->
        response.Headers
        |> Headers.tryFind "Set-Cookie" []
        |> Option.map (fun cookie ->
            let headers = Map [ "Cookie", cookie ] |> Some
            httpClient |> Headers.set headers)
        |> function
            | Some result -> result |> Result.map (fun _ -> response)
            | None -> Error <| NotFound "Session cookie not found in response headers."

let solveCaptcha ct siteUri siteKey =
    Web.AntiCaptcha.ReCaptcha.V3.Enterprise.fromPage ct siteUri siteKey

let buildFormData (credentials: Credentials) recaptchaToken =
    Map [
        "RECAPTCHA", recaptchaToken
        "Email", credentials.Login
        "Password", credentials.Password
    ]

let postLoginPage ct formData =
    fun httpClient ->
        httpClient
        |> Request.post
            { Path = "/Home/Login"; Headers = None }
            (Content.String {|
                Data = formData |> FormData.build
                Encoding = Text.Encoding.UTF8
                ContentType = "application/x-www-form-urlencoded"
            |})
            ct
        |> Response.String.read ct

let setAuthCookie (response: Response<string>) =
    fun httpClient ->
        response.Headers
        |> Headers.tryFind "Set-Cookie" []
        |> Option.map (fun cookie ->
            let headers = Map [ "Cookie", cookie ] |> Some
            httpClient |> Headers.set headers)
        |> function
            | Some result -> result |> Result.map ignore
            | None -> Error <| NotFound "Auth cookie not found in response headers."

let getServicePage ct serviceId =
    fun httpClient ->
        match serviceId |> Router.parse with
        | Ok(Visa service) ->
            match service with
            | Visa.Tourism1 _ -> "/Services/Booking/1151" |> Ok
            | Visa.Tourism2 _ -> "/Services/Booking/1258" |> Ok
        | Error _ ->
            $"The service Id '{serviceId}' is not recognized to process prenotami."
            |> NotFound
            |> Error
        |> ResultAsync.wrap (fun path ->
            httpClient
            |> Request.get { Path = path; Headers = None } ct
            |> Response.String.read ct)
