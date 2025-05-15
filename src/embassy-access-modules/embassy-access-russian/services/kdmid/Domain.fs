module EA.Russian.Services.Domain.Kdmid

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients
open Web.Clients.Domain
open EA.Core.Domain

module Constants =
    module ErrorCode =
        [<Literal>]
        let PAGE_HAS_ERROR = "PageHasError"

        [<Literal>]
        let REQUEST_NOT_CONFIRMED = "RequestNotConfirmed"

        [<Literal>]
        let REQUEST_AWAITING_LIST = "RequestAwaitingList"

        [<Literal>]
        let REQUEST_DELETED = "RequestDeleted"

        [<Literal>]
        let REQUEST_BLOCKED = "RequestBlocked"

        [<Literal>]
        let REQUEST_NOT_FOUND = "RequestNotFound"

        [<Literal>]
        let INITIAL_PAGE_ERROR = "InitialPageError"

let private result = ResultBuilder()

type Credentials = {
    Id: int
    Cd: string
    Ems: string option
    Subdomain: string
} with

    static member parse(payload: string) =
        result {
            let! uri = payload |> Web.Clients.Http.Route.toUri

            let! hostParts =
                match uri.Host.Split '.' with
                | hostParts when hostParts.Length < 3 ->
                    $"Kdmid host: '%s{uri.Host}' is not supported." |> NotSupported |> Error
                | hostParts -> hostParts |> Ok

            let subdomain = hostParts[0]

            let! queryParams = uri |> Http.Route.toQueryParams

            let! id =
                queryParams
                |> Map.tryFind "id"
                |> Option.map (function
                    | AP.IsInt id when id > 1000 -> id |> Ok
                    | _ -> "Kdmid payload 'ID' query parameter is not supported." |> NotSupported |> Error)
                |> Option.defaultValue ("Kdmid payload 'ID' query parameter not found." |> NotFound |> Error)

            let! cd =
                queryParams
                |> Map.tryFind "cd"
                |> Option.map (function
                    | AP.IsLettersOrNumbers cd -> cd |> Ok
                    | _ -> "Kdmid payload 'CD' query parameter is not supported." |> NotSupported |> Error)
                |> Option.defaultValue ("Kdmid payload 'CD' query parameter not found." |> NotFound |> Error)

            let! ems =
                queryParams
                |> Map.tryFind "ems"
                |> Option.map (function
                    | AP.IsLettersOrNumbers ems -> ems |> Some |> Ok
                    | _ -> "Kdmid payload 'EMS' query parameter is not supported." |> NotSupported |> Error)
                |> Option.defaultValue (None |> Ok)

            return {
                Subdomain = subdomain
                Id = id
                Cd = cd
                Ems = ems
            }
        }

    static member print(payload: Credentials) =
        let ems =
            payload.Ems
            |> Option.map (fun ems -> $"\n ems=%s{ems}")
            |> Option.defaultValue ""
        $"' id=%i{payload.Id}\n cd=%s{payload.Cd}{ems}'"

type PayloadState =
    | NoAppointments
    | HasAppointments of Set<Appointment>
    | HasConfirmation of string * Appointment

    static member print(payloadState: PayloadState) =
        match payloadState with
        | NoAppointments -> "No appointments found."
        | HasAppointments appointments -> appointments |> Seq.map Appointment.print |> String.concat "\n "
        | HasConfirmation(message, appointment) -> $"The appointment '%s{appointment.Value}' is confirmed: %s{message}"
        |> sprintf "[Last request state]\n %s"

type Payload = {
    Credentials: Credentials
    Confirmation: Confirmation
    State: PayloadState
} with

    static member print(payload: Payload) =
        payload.Credentials
        |> Credentials.print
        |> fun credentials -> $"[Credentials]\n %s{credentials}\n{PayloadState.print payload.State}"

    static member printError (error: Error') (payload: Payload) =
        match error with
        | Operation reason ->
            match reason.Code with
            | Some(Custom Constants.ErrorCode.REQUEST_AWAITING_LIST)
            | Some(Custom Constants.ErrorCode.REQUEST_NOT_CONFIRMED)
            | Some(Custom Constants.ErrorCode.REQUEST_BLOCKED)
            | Some(Custom Constants.ErrorCode.REQUEST_NOT_FOUND)
            | Some(Custom Constants.ErrorCode.REQUEST_DELETED) -> error.Message |> Some
            | _ -> None
        | _ -> None
        |> Option.map (fun error ->
            payload.Credentials
            |> Credentials.print
            |> fun credentials -> credentials + Environment.NewLine + error)

type Client = {
    initHttpClient: string -> Result<Http.Client, Error'>
    updateRequest: Request<Payload> -> Async<Result<Request<Payload>, Error'>>
    getCaptcha: Http.Request -> Http.Client -> Async<Result<Http.Response<byte array>, Error'>>
    solveIntCaptcha: byte array -> Async<Result<int, Error'>>
    getInitialPage: Http.Request -> Http.Client -> Async<Result<Http.Response<string>, Error'>>
    postValidationPage: Http.Request -> Http.RequestContent -> Http.Client -> Async<Result<string, Error'>>
    postAppointmentsPage: Http.Request -> Http.RequestContent -> Http.Client -> Async<Result<string, Error'>>
    postConfirmationPage: Http.Request -> Http.RequestContent -> Http.Client -> Async<Result<string, Error'>>
}
