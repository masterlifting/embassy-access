module EA.Italian.Services.Domain.Prenotami

open System
open System.Threading
open Web.Clients.Domain
open Infrastructure.Prelude
open Infrastructure.SerDe
open EA.Core.Domain
open EA.Core.DataAccess
open Infrastructure.Domain

module Constants =
    module ErrorCode =
        [<Literal>]
        let PAGE_HAS_ERROR = "PageHasError"

        [<Literal>]
        let INITIAL_PAGE_ERROR = "InitialPageError"

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

type Payload = {
    Credentials: Credentials
    Appointments: Set<Appointment>
} with

    static member print(payload: Payload) =
        payload.Credentials
        |> Credentials.print
        |> fun v ->
            v
            + Environment.NewLine
            + match payload.Appointments.IsEmpty with
              | true -> "No appointments found."
              | false ->
                  payload.Appointments
                  |> Seq.map (fun appointment -> appointment |> Appointment.print)
                  |> String.concat ", "
                  |> fun appointments -> $"Appointments: '%s{appointments}'"

    static member printError (error: Error') =
        match error with
        | Operation reason ->
            match reason.Code with
            | Some(Custom Constants.ErrorCode.INITIAL_PAGE_ERROR) -> error.Message |> Some
            | _ -> None
        | _ -> None

    static member serialize key (payload: Payload) =
        payload.Credentials.Password
        |> String.encrypt key
        |> Result.bind (fun password ->
            Json.serialize {
                payload with
                    Payload.Credentials.Password = password
            })

    static member deserialize key (payload: string) =
        payload
        |> Json.deserialize<Payload>
        |> Result.bind (fun payload ->
            payload.Credentials.Password
            |> String.decrypt key
            |> Result.map (fun password -> {
                payload with
                    Payload.Credentials.Password = password
            }))

type Client = {
    initHttpClient: Credentials -> Result<Http.Client, Error'>
    updateRequest: Request<Payload> -> Async<Result<Request<Payload>, Error'>>
    getInitialPage: Http.Request -> Http.Client -> Async<Result<Http.Response<string>, Error'>>
}

type Dependencies = {
    RequestStorage: Request.Storage<Payload>
    CancellationToken: CancellationToken
}
