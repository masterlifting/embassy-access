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
open EA.Italian.Services.Router
open EA.Italian.Services.Domain.Prenotami
open EA.Italian.Services.DataAccess.Prenotami
open EA.Telegram.Dependencies

type Dependencies = {
    ct: CancellationToken
    ChatId: Telegram.ChatId
    MessageId: int
    findService: ServiceId -> Async<Result<Tree.Node<Service>, Error'>>
    findEmbassy: EmbassyId -> Async<Result<Tree.Node<Embassy>, Error'>>
    processRequest: Request<Payload> -> StorageType -> Async<Result<Request<Payload>, Error'>>
    findRequest: RequestId -> StorageType -> Async<Result<Request<Payload>, Error'>>
    tryFindRequest:
        EmbassyId -> ServiceId -> Credentials -> StorageType -> Async<Result<Request<Payload> option, Error'>>
    findRequests: EmbassyId -> ServiceId -> StorageType -> Async<Result<Request<Payload> list, Error'>>
    deleteRequest: RequestId -> StorageType -> Async<Result<unit, Error'>>
    createOrUpdateRequest: Request<Payload> -> StorageType -> Async<Result<unit, Error'>>
    tryAddSubscription: Request<Payload> -> Async<Result<unit, Error'>>
    sendTranslatedMessageRes: Async<Result<Telegram.Producer.Message, Error'>> -> Async<Result<unit, Error'>>
    initRequestStorage: unit -> Result<StorageType, Error'>
} with

    static member create(deps: Italian.Dependencies) =

        let findRequest requestId storage =
            storage
            |> Storage.Request.Query.findOne (Storage.Request.Query.Id requestId)
            |> ResultAsync.bind (function
                | Some request -> Ok request
                | None -> $"Subscription '{requestId.ValueStr}' not found." |> NotFound |> Error)

        let tryFindRequest embassyId serviceId credentials storage =

            let compareServices route1 route2 =
                match route1, route2 with
                | Visa route1, Visa route2 ->
                    match route1, route2 with
                    | Visa.Tourism1 _, Visa.Tourism1 _ -> true
                    | Visa.Tourism2 _, Visa.Tourism2 _ -> true
                    | _ -> false

            serviceId
            |> Router.parse
            |> ResultAsync.wrap (fun route1 ->
                storage
                |> Storage.Request.Query.findMany (Storage.Request.Query.ByEmbassyId embassyId)
                |> ResultAsync.map (Seq.filter (fun request -> request.Payload.Credentials.Login = credentials.Login))
                |> ResultAsync.map (
                    Seq.tryFind (fun request ->
                        match request.Service.Id |> ServiceId |> Router.parse with
                        | Ok route2 -> compareServices route1 route2
                        | _ -> false)
                ))

        let findRequests embassyId serviceId storage =
            storage
            |> Storage.Request.Query.findMany (Storage.Request.Query.ByEmbassyAndServiceId(embassyId, serviceId))

        let deleteRequest requestId storage =
            storage
            |> Storage.Request.Command.delete requestId
            |> ResultAsync.bindAsync (fun _ -> deps.deleteSubscription requestId)

        let createOrUpdateRequest request storage =
            storage |> Storage.Request.Command.upsert request |> ResultAsync.map ignore

        let processRequest request storage =
            Prenotami.Client.init {
                ct = deps.ct
                BrowserWebApiUrl = Configuration.ENVIRONMENTS.BrowserWebApiUrl
                RequestStorage = storage
            }
            |> Prenotami.Service.tryProcess request

        let tryAddSubscription (request: Request<Payload>) =
            let serviceId = request.Service.Id |> ServiceId
            let embassyId = request.Embassy.Id |> EmbassyId
            deps.tryAddSubscription request.Id serviceId embassyId

        {
            ct = deps.ct
            ChatId = deps.Chat.Id
            MessageId = deps.MessageId
            initRequestStorage = deps.initPrenotamiRequestStorage
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

module ProcessResult =
    type Dependencies = {
        getChats: RequestId seq -> Async<Result<Chat list, Error'>>
        getRequests: EmbassyId -> ServiceId -> Async<Result<Request<Payload> list, Error'>>
        updateRequests: Request<Payload> seq -> Async<Result<Request<Payload> list, Error'>>
        spreadTranslatedMessages: (Culture * Telegram.Producer.Message) seq -> Async<Result<unit, Error'>>
    }
