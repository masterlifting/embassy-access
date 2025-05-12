[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Russian.Kdmid

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.Dependencies.Services.Russian
open EA.Russian.Services
open EA.Russian.Services.Domain.Kdmid
open EA.Russian.Services.DataAccess.Kdmid

type Dependencies = {
    ChatId: Telegram.ChatId
    MessageId: int
    findService: ServiceId -> Async<Result<Service, Error'>>
    findEmbassy: EmbassyId -> Async<Result<Embassy, Error'>>
    processRequest:
        Request<Payload> -> Request.Storage<Payload, Payload.Entity> -> Async<Result<Request<Payload>, Error'>>
    findRequest: RequestId -> Request.Storage<Payload, Payload.Entity> -> Async<Result<Request<Payload>, Error'>>
    tryFindRequest:
        EmbassyId
            -> Credentials
            -> Request.Storage<Payload, Payload.Entity>
            -> Async<Result<Request<Payload> option, Error'>>
    findRequests:
        EmbassyId
            -> ServiceId
            -> Request.Storage<Payload, Payload.Entity>
            -> Async<Result<Request<Payload> list, Error'>>
    deleteRequest: RequestId -> Request.Storage<Payload, Payload.Entity> -> Async<Result<unit, Error'>>
    createOrUpdateRequest: Request<Payload> -> Request.Storage<Payload, Payload.Entity> -> Async<Result<unit, Error'>>
    tryAddSubscription: Request<Payload> -> Async<Result<unit, Error'>>
    sendTranslatedMessageRes: Async<Result<Telegram.Producer.Message, Error'>> -> Async<Result<unit, Error'>>
    initRequestStorage: unit -> Result<Request.Storage<Payload, Payload.Entity>, Error'>
} with

    static member create(deps: Russian.Dependencies) =

        let findRequest requestId storage =
            storage
            |> Storage.Request.Query.tryFind (Storage.Request.Query.Id requestId)
            |> ResultAsync.bind (function
                | Some request -> Ok request
                | None -> $"Request '{requestId.ValueStr}' not found." |> NotFound |> Error)

        let tryFindRequest embassyId credentials storage =
            storage
            |> Storage.Request.Query.findMany (Storage.Request.Query.ByEmbassyId embassyId)
            |> ResultAsync.map (Seq.tryFind (fun request -> request.Payload.Credentials = credentials))

        let findRequests embassyId serviceId storage =
            storage
            |> Storage.Request.Query.findMany (Storage.Request.Query.ByEmbassyAndServiceId(embassyId, serviceId))

        let createOrUpdateRequest request storage =
            storage
            |> Storage.Request.Command.createOrUpdate request
            |> ResultAsync.map ignore

        let deleteRequest requestId storage =
            storage
            |> Storage.Request.Command.delete requestId
            |> ResultAsync.bindAsync (fun _ -> deps.deleteSubscription requestId)

        let processRequest request storage =
            Kdmid.Client.init {
                ct = deps.ct
                RequestStorage = storage
            }
            |> ResultAsync.wrap (Kdmid.Service.tryProcess request)

        let tryAddSubscription (request: Request<Payload>) =
            deps.tryAddSubscription request.Id request.Service.Id request.Embassy.Id

        {
            ChatId = deps.Chat.Id
            MessageId = deps.MessageId
            initRequestStorage = deps.initKdmidRequestStorage
            findService = deps.findService
            findEmbassy = deps.findEmbassy
            findRequest = findRequest
            tryFindRequest = tryFindRequest
            findRequests = findRequests
            deleteRequest = deleteRequest
            tryAddSubscription = tryAddSubscription
            processRequest = processRequest
            createOrUpdateRequest = createOrUpdateRequest
            sendTranslatedMessageRes = deps.sendTranslatedMessageRes
        }

module Notification =
    type Dependencies = {
        getRequestChats: Request<Payload> -> Async<Result<Chat list, Error'>>
        setRequestsAppointments:
            EmbassyId -> ServiceId -> Appointment Set -> Async<Result<Request<Payload> list, Error'>>
        spreadTranslatedMessages: (Culture * Telegram.Producer.Message) seq -> Async<Result<unit, Error'>>
    }
