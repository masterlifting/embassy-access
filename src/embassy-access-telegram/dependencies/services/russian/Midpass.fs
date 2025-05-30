﻿[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Russian.Midpass

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Web.Clients.Domain
open EA.Core.DataAccess
open EA.Telegram.Dependencies.Services.Russian
open EA.Russian.Services.Domain.Midpass
open EA.Russian.Services.DataAccess.Midpass

type Dependencies = {
    ChatId: Telegram.ChatId
    MessageId: int
    tryFindServiceNode: ServiceId -> Async<Result<Graph.Node<Service> option, Error'>>
    tryFindEmbassyNode: EmbassyId -> Async<Result<Graph.Node<Embassy> option, Error'>>
    sendTranslatedMessageRes: Async<Result<Telegram.Producer.Message, Error'>> -> Async<Result<unit, Error'>>
    findRequest: RequestId -> StorageType -> Async<Result<Request<Payload>, Error'>>
    findRequests: EmbassyId -> ServiceId -> StorageType -> Async<Result<Request<Payload> list, Error'>>
    deleteRequest: RequestId -> StorageType -> Async<Result<unit, Error'>>
    initRequestStorage: unit -> Result<StorageType, Error'>
} with

    static member create(deps: Russian.Dependencies) =

        let findRequest requestId storage =
            storage
            |> Storage.Request.Query.tryFind (Storage.Request.Query.Id requestId)
            |> ResultAsync.bind (function
                | Some request -> Ok request
                | None -> $"Subscription '{requestId.ValueStr}' not found." |> NotFound |> Error)

        let findRequests embassyId serviceId storage =
            storage
            |> Storage.Request.Query.findMany (Storage.Request.Query.ByEmbassyAndServiceId(embassyId, serviceId))

        let deleteRequest requestId storage =
            storage
            |> Storage.Request.Command.delete requestId
            |> ResultAsync.bindAsync (fun _ -> deps.deleteSubscription requestId)

        {
            ChatId = deps.Chat.Id
            MessageId = deps.MessageId
            tryFindServiceNode = deps.tryFindServiceNode
            tryFindEmbassyNode = deps.tryFindEmbassyNode
            sendTranslatedMessageRes = deps.sendTranslatedMessageRes
            findRequest = findRequest
            findRequests = findRequests
            deleteRequest = deleteRequest
            initRequestStorage = deps.initMidpassRequestStorage
        }
