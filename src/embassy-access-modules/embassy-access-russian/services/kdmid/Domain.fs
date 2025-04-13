module EA.Russian.Services.Domain.Kdmid

open System
open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open Web.Clients
open Web.Clients.Domain

let private result = ResultBuilder()

type Client = {
    initHttpClient: string -> Result<Http.Client, Error'>
    updateRequest: Request -> Async<Result<Request, Error'>>
    getCaptcha: Http.Request -> Http.Client -> Async<Result<Http.Response<byte array>, Error'>>
    solveIntCaptcha: byte array -> Async<Result<int, Error'>>
    getInitialPage: Http.Request -> Http.Client -> Async<Result<Http.Response<string>, Error'>>
    postValidationPage: Http.Request -> Http.RequestContent -> Http.Client -> Async<Result<string, Error'>>
    postAppointmentsPage: Http.Request -> Http.RequestContent -> Http.Client -> Async<Result<string, Error'>>
    postConfirmationPage: Http.Request -> Http.RequestContent -> Http.Client -> Async<Result<string, Error'>>
}

type Dependencies = {
    RequestStorage: Request.RequestStorage
    CancellationToken: CancellationToken
}

type Payload = {
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

    static member print(payload: Payload) =
        let ems =
            payload.Ems
            |> Option.map (fun ems -> $"; EMS:%s{ems}")
            |> Option.defaultValue ""
        $"'ID:%i{payload.Id}; CD:%s{payload.Cd}{ems} (%s{payload.Subdomain})'"

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
