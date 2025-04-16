module EA.Italian.Services.Domain.Prenotami

open System
open System.Threading
open EA.Core.Domain
open EA.Core.DataAccess
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain

type Credentials = {
    Login: string
    Password: string
} with

    static member create(payload: string) =
        match payload with
        | AP.IsString v ->
            let parts = v.Split ';'
            match parts.Length = 2 with
            | true ->
                let login = parts[0]
                let password = parts[1]
                { Login = login; Password = password } |> Ok
            | false -> $"Prenotami payload: '%s{payload}' is not supported." |> NotSupported |> Error
        | _ -> $"Prenotami payload: '%s{payload}' is not supported." |> NotSupported |> Error

    static member print(payload: Credentials) = $"Login: '%s{payload.Login}'"

type Request = {
    Credentials: Credentials
    Appointments: Set<Appointment>
} with

    static member print(request: Request) =
        request.Credentials
        |> Credentials.print
        |> fun v ->
            v
            + Environment.NewLine
            + match request.Appointments.IsEmpty with
              | true -> "No appointments found"
              | false ->
                  request.Appointments
                  |> Seq.map (fun appointment -> appointment |> Appointment.print)
                  |> String.concat ", "
                  |> fun appointments -> $"Appointments: '%s{appointments}'"

type Client = {
    initHttpClient: Credentials -> Result<Http.Client, Error'>
    updateRequest: Request'<Request> -> Async<Result<Request'<Request>, Error'>>
    getInitialPage: Http.Request -> Http.Client -> Async<Result<Http.Response<string>, Error'>>
}

type Dependencies = {
    RequestStorage: Request.RequestStorage
    CancellationToken: CancellationToken
}

module Constants =
    module ErrorCode =
        [<Literal>]
        let PAGE_HAS_ERROR = "PageHasError"

        [<Literal>]
        let INITIAL_PAGE_ERROR = "InitialPageError"
