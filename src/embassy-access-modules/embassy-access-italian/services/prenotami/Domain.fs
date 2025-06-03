module EA.Italian.Services.Domain.Prenotami

open System
open System.Text.RegularExpressions
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain
open EA.Core.Domain

module Constants =
    module ErrorCode =

        [<Literal>]
        let PAGE_HAS_ERROR = "Page has an error"

type Credentials = {
    Login: string
    Password: string
} with

    static member parse (login: string) (password: string) =
        match login with
        | AP.IsEmail l ->
            match password with
            | AP.IsString p -> { Login = l; Password = p } |> Ok
            | _ -> $"Prenotami password: '%s{password}' is not valid." |> NotSupported |> Error
        | _ -> $"Prenotami login: '%s{login}' is not valid." |> NotSupported |> Error

    static member createBasicAuth(credentials: Credentials) =
        try
            let auth =
                Convert.ToBase64String(Text.Encoding.UTF8.GetBytes(credentials.Login + ":" + credentials.Password))
            $"Basic {auth}" |> Ok
        with ex ->
            Operation {
                Message = "Error creating Basic Auth. " + (ex |> Exception.toMessage)
                Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
            }
            |> Error

    static member print(credentials: Credentials) =
        $"Login: '%s{credentials.Login}'\n Password: '%s{credentials.Password}'"

type internal Appointment with
    static member parse(text: string) =
        {
            Id = AppointmentId.createNew ()
            Value = text
            Date = DateOnly.FromDateTime DateTime.UtcNow
            Time = TimeOnly.FromDateTime DateTime.UtcNow
            Description = text
        }
        |> Set.singleton
        |> Ok

type PayloadState =
    | NoAppointments
    | HasAppointments of Set<Appointment>

    static member print(payloadState: PayloadState) =
        match payloadState with
        | NoAppointments -> "No appointments found."
        | HasAppointments appointments -> appointments |> Seq.map Appointment.print |> String.concat "\n "

type Payload = {
    Credentials: Credentials
    State: PayloadState
} with

    static member printError (error: Error') (payload: Payload) =
        match error with
        | Operation reason ->
            match reason.Code with
            | Some(Custom Constants.ErrorCode.PAGE_HAS_ERROR) -> None
            | _ -> error.Message |> Some
        | _ -> error.Message |> Some
        |> Option.map (fun error ->
            payload.Credentials
            |> Credentials.print
            |> fun credentials -> credentials + Environment.NewLine + error)

type Client = {
    updateRequest: Request<Payload> -> Async<Result<Request<Payload>, Error'>>
    Browser: {|
        loadPage: Uri -> Async<Result<Browser.Page, Error'>>
        closePage: Browser.Page -> Async<Result<unit, Error'>>
        fillInput: Browser.Selector -> string -> Browser.Page -> Async<Result<Browser.Page, Error'>>
        mouseClick: Browser.Selector -> Browser.Mouse.WaitFor -> Browser.Page -> Async<Result<Browser.Page, Error'>>
        submitForm: Browser.Selector -> Regex -> Browser.Page -> Async<Result<Browser.Page, Error'>>
        tryFindText: Browser.Selector -> Browser.Page -> Async<Result<string option, Error'>>
    |}
}

type ClientNew = {
    initHttpClient: unit -> Async<Result<Http.Client, Error'>>
    updateRequest: Request<Payload> -> Async<Result<Request<Payload>, Error'>>
    getInitialPage: unit -> Async<Result<Http.Response<string>, Error'>>
    setSessionCookie: Http.Response<string> -> Result<Http.Response<string>, Error'>
    solveCaptcha: string -> Async<Result<string, Error'>>
    buildFormData: string -> Map<string, string>
    postLoginPage: Map<string, string> -> Async<Result<Http.Response<string>, Error'>>
    setAuthCookie: Http.Response<string> -> Result<unit, Error'>
    getServicePage: unit -> Async<Result<Http.Response<string>, Error'>>
}
