﻿module EA.Russian.Services.Domain.Kdmid

open System
open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.SerDe
open EA.Core.Domain
open EA.Core.DataAccess
open Web.Clients
open Web.Clients.Domain

let private result = ResultBuilder()
type Credentials = {
    Subdomain: string
    Id: int
    Cd: string
    Ems: string option
} with

    static member create(payload: string) =
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
            |> Option.map (fun ems -> $"; EMS:%s{ems}")
            |> Option.defaultValue ""
        $"'ID:%i{payload.Id}; CD:%s{payload.Cd}{ems} (%s{payload.Subdomain})'"

type Payload = {
    Credentials: Credentials
    Confirmation: Confirmation
    Appointments: Set<Appointment>
} with

    static member print(payload: Payload) =
        payload.Credentials
        |> Credentials.print
        |> fun v ->
            v
            + Environment.NewLine
            + match payload.Appointments.IsEmpty with
              | true -> "No appointments found"
              | false ->
                  payload.Appointments
                  |> Seq.map (fun appointment -> appointment |> Appointment.print)
                  |> String.concat Environment.NewLine
                  
    static member serialize (payload: Payload) =
        payload |> Json.serialize
        
    static member deserialize (payload: string) =
        payload |> Json.deserialize<Payload>

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

type Dependencies = {
    RequestsTable: Request.Table<Payload>
    CancellationToken: CancellationToken
}


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
