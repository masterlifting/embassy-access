[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Russian.Kdmid

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Russian.Services
open EA.Russian.Services.Domain.Kdmid
open EA.Telegram.Dependencies.Services.Russian

type Dependencies = {
    ct: CancellationToken
    ChatId: Telegram.ChatId
    MessageId: int
    initKdmidRequestStorage: unit -> Result<Request.Storage<Payload>, Error'>
    findService: ServiceId -> Async<Result<Service, Error'>>
    findEmbassy: EmbassyId -> Async<Result<Embassy, Error'>>
    processRequest: Request<Payload> -> Request.Storage<Payload> -> Async<Result<Request<Payload>, Error'>>
    findRequests: EmbassyId -> ServiceId -> Request.Storage<Payload> -> Async<Result<Request<Payload> list, Error'>>
    createOrUpdateRequest: Request<Payload> -> Request.Storage<Payload> -> Async<Result<unit, Error'>>
    sendTranslatedMessageRes: Async<Result<Telegram.Producer.Message, Error'>> -> Async<Result<unit, Error'>>
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

        let findRequests embassyId serviceId storage =
            storage
            |> Storage.Request.Query.findMany (Storage.Request.Query.ByEmbassyAndServiceId(embassyId, serviceId))

        // TODO: Improve this with additional error handling
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

        {
            ct = deps.ct
            ChatId = deps.Chat.Id
            MessageId = deps.MessageId
            initKdmidRequestStorage = deps.initKdmidRequestStorage
            findService = findService
            findEmbassy = findEmbassy
            findRequests = findRequests
            processRequest = processRequest
            createOrUpdateRequest = createOrUpdateRequest
            sendTranslatedMessageRes = deps.sendTranslatedMessageRes
        }

module Notification =
    open EA.Telegram.Domain

    type Dependencies = {
        getRequestChats: Request<Payload> -> Async<Result<Chat list, Error'>>
        setAppointments: ServiceId -> Appointment Set -> Async<Result<Request<Payload> list, Error'>>
        sendTranslatedMessagesRes: Chat -> Telegram.Producer.Message seq -> Async<Result<unit, Error'>>
    }
