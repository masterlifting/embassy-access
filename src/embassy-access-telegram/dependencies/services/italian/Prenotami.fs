[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Italian.Prenotami

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.Dependencies.Services.Italian
open EA.Italian.Services
open EA.Italian.Services.Domain.Prenotami
open EA.Italian.Services.DataAccess.Prenotami

type Dependencies = {
    ct: CancellationToken
    ChatId: Telegram.ChatId
    MessageId: int
    findService: ServiceId -> Async<Result<Service, Error'>>
    findEmbassy: EmbassyId -> Async<Result<Embassy, Error'>>
    processRequest:
        Request<Payload> -> Request.Storage<Payload, Payload.Entity> -> Async<Result<Request<Payload>, Error'>>
    findRequest: RequestId -> Request.Storage<Payload, Payload.Entity> -> Async<Result<Request<Payload>, Error'>>
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

    static member create(deps: Italian.Dependencies) =

        let findRequest requestId storage =
            storage
            |> Storage.Request.Query.tryFind (Storage.Request.Query.Id requestId)
            |> ResultAsync.bind (function
                | Some request -> Ok request
                | None -> $"Request '{requestId.ValueStr}' not found." |> NotFound |> Error)

        let tryFindRequest embassyId serviceId credentials storage =
            storage
            |> Storage.Request.Query.findMany (Storage.Request.Query.ByEmbassyId embassyId)
            |> ResultAsync.map (Seq.tryFind (fun request -> request.Payload.Credentials.Login = credentials.Login))

        let findRequests embassyId serviceId storage =
            storage
            |> Storage.Request.Query.findMany (Storage.Request.Query.ByEmbassyAndServiceId(embassyId, serviceId))

        let deleteRequest requestId storage =
            storage
            |> Storage.Request.Command.delete requestId
            |> ResultAsync.bindAsync (fun _ -> deps.deleteSubscription requestId)

        let createOrUpdateRequest request storage =
            storage
            |> Storage.Request.Command.createOrUpdate request
            |> ResultAsync.map ignore

        let processRequest request storage =
            Prenotami.Client.init {
                ct = deps.ct
                RequestStorage = storage
            }
            |> ResultAsync.bindAsync (Prenotami.Service.tryProcess request)

        let tryAddSubscription (request: Request<Payload>) =
            deps.tryAddSubscription request.Id request.Service.Id request.Embassy.Id

        {
            ct = deps.ct
            ChatId = deps.Chat.Id
            MessageId = deps.MessageId
            initRequestStorage = deps.initPrenotamiRequestStorage
            findService = deps.findService
            findEmbassy = deps.findEmbassy
            findRequest = findRequest
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
