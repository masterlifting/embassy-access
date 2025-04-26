module EA.Italian.Services.Domain.Prenotami

open System
open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain
open EA.Core.Domain
open EA.Core.DataAccess

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

    static member parse (login: string) (password: string) =
        match login, password with
        | AP.IsString l, AP.IsString p -> { Login = l; Password = p } |> Ok
        | _ ->
            $"Prenotami credentials: '%s{login},{password}' is not supported."
            |> NotSupported
            |> Error

    static member print(payload: Credentials) = $"Login: '%s{payload.Login}'"

type PayloadState =
    | NoAppointments
    | HasAppointments of Set<Appointment>

    static member print(payloadState: PayloadState) =
        match payloadState with
        | NoAppointments -> "No appointments found."
        | HasAppointments appointments -> appointments |> Seq.map Appointment.print |> String.concat Environment.NewLine

type Payload = {
    Credentials: Credentials
    State: PayloadState
} with

    static member print(payload: Payload) =
        payload.Credentials
        |> Credentials.print
        |> fun credentials -> credentials + Environment.NewLine + PayloadState.print payload.State

    static member printError (error: Error') (payload: Payload) =
        match error with
        | Operation reason ->
            match reason.Code with
            | Some(Custom Constants.ErrorCode.INITIAL_PAGE_ERROR) -> error.Message |> Some
            | _ -> None
        | _ -> None
        |> Option.map (fun error ->
            payload.Credentials
            |> Credentials.print
            |> fun credentials -> credentials + Environment.NewLine + error)

type Client = {
    initHttpClient: Credentials -> Result<Http.Client, Error'>
    updateRequest: Request<Payload> -> Async<Result<Request<Payload>, Error'>>
    getInitialPage: Http.Request -> Http.Client -> Async<Result<Http.Response<string>, Error'>>
}

type Dependencies = {
    ct: CancellationToken
    RequestStorage: Request.Storage<Payload>
}
