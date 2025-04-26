[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Russian.Kdmid

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies.Services.Russian
open EA.Russian.Services
open EA.Russian.Services.Domain.Kdmid

type Dependencies = {
    ct: CancellationToken
    ChatId: Telegram.ChatId
    MessageId: int
    findService: ServiceId -> Async<Result<Service, Error'>>
    findEmbassy: EmbassyId -> Async<Result<Embassy, Error'>>
    processRequest: Request<Payload> -> Request.Storage<Payload> -> Async<Result<Request<Payload>, Error'>>
    findRequest: RequestId -> Request.Storage<Payload> -> Async<Result<Request<Payload>, Error'>>
    findRequests: EmbassyId -> ServiceId -> Request.Storage<Payload> -> Async<Result<Request<Payload> list, Error'>>
    createOrUpdateRequest: Request<Payload> -> Request.Storage<Payload> -> Async<Result<unit, Error'>>
    tryUpdateChatSubscriptions: Request<Payload> -> Async<Result<unit, Error'>>
    sendTranslatedMessageRes: Async<Result<Telegram.Producer.Message, Error'>> -> Async<Result<unit, Error'>>
    initKdmidRequestStorage: unit -> Result<Request.Storage<Payload>, Error'>
} with

    static member create(deps: Russian.Dependencies) =

        let findService serviceId =
            deps.tryFindServiceNode serviceId
            |> ResultAsync.bind (function
                | Some node -> node.Value |> Ok
                | None -> $"Service '{serviceId.ValueStr}' not found." |> NotFound |> Error)

        let findEmbassy embassyId =
            deps.tryFindEmbassyNode embassyId
            |> ResultAsync.bind (function
                | Some node -> node.Value |> Ok
                | None -> $"Embassy '{embassyId.ValueStr}' not found." |> NotFound |> Error)

        let findRequest requestId storage =
            storage
            |> Storage.Request.Query.tryFind (Storage.Request.Query.Id requestId)
            |> ResultAsync.bind (function
                | Some request -> Ok request
                | None -> $"Request '{requestId.ValueStr}' not found." |> NotFound |> Error)

        let findRequests embassyId serviceId storage =
            storage
            |> Storage.Request.Query.findMany (Storage.Request.Query.ByEmbassyAndServiceId(embassyId, serviceId))

        let createOrUpdateRequest request storage =
            storage
            |> Storage.Request.Command.createOrUpdate request
            |> ResultAsync.map ignore

        let processRequest request storage =
            Kdmid.Client.init {
                ct = deps.ct
                RequestStorage = storage
            }
            |> ResultAsync.wrap (Kdmid.Service.tryProcess request)

        let tryUpdateChatSubscriptions (request: Request<Payload>) =
            match deps.Chat.Subscriptions |> Set.exists (fun s -> s.Id = request.Id) with
            | true -> Ok() |> async.Return
            | false ->
                deps.initChatStorage ()
                |> ResultAsync.wrap (
                    Storage.Chat.Command.update {
                        deps.Chat with
                            Subscriptions =
                                deps.Chat.Subscriptions
                                |> Set.add {
                                    Id = request.Id
                                    ServiceId = request.Service.Id
                                    EmbassyId = request.Embassy.Id
                                }
                    }
                )
                |> ResultAsync.map ignore

        {
            ct = deps.ct
            ChatId = deps.Chat.Id
            MessageId = deps.MessageId
            initKdmidRequestStorage = deps.initKdmidRequestStorage
            findService = findService
            findEmbassy = findEmbassy
            findRequest = findRequest
            findRequests = findRequests
            tryUpdateChatSubscriptions = tryUpdateChatSubscriptions
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
