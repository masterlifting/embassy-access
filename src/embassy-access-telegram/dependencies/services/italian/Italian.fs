﻿module EA.Telegram.Dependencies.Services.Italian.Italian

open System.Threading
open Infrastructure.Domain
open Web.Clients.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Services
open EA.Italian.Services.Domain
open EA.Italian.Services.DataAccess

type Dependencies = {
    ct: CancellationToken
    Chat: Chat
    MessageId: int
    initWebBrowser: unit -> Async<Result<Browser.Client, Error'>>
    tryFindServiceNode: ServiceId -> Async<Result<Graph.Node<Service> option, Error'>>
    tryFindEmbassyNode: EmbassyId -> Async<Result<Graph.Node<Embassy> option, Error'>>
    findService: ServiceId -> Async<Result<Service, Error'>>
    findEmbassy: EmbassyId -> Async<Result<Embassy, Error'>>
    tryAddSubscription: RequestId -> ServiceId -> EmbassyId -> Async<Result<unit, Error'>>
    deleteSubscription: RequestId -> Async<Result<unit, Error'>>
    initChatStorage: unit -> Result<Chat.Storage, Error'>
    initPrenotamiRequestStorage: unit -> Result<Request.Storage<Prenotami.Payload, Prenotami.Payload.Entity>, Error'>
    sendTranslatedMessageRes: Async<Result<Telegram.Producer.Message, Error'>> -> Async<Result<unit, Error'>>
} with

    static member create(deps: Services.Dependencies) = {
        ct = deps.ct
        Chat = deps.Chat
        MessageId = deps.MessageId
        initWebBrowser = deps.Request.initWebBrowser
        tryFindServiceNode = deps.tryFindServiceNode
        tryFindEmbassyNode = deps.tryFindEmbassyNode
        findService = deps.findService
        findEmbassy = deps.findEmbassy
        tryAddSubscription = deps.tryAddSubscription
        deleteSubscription = deps.deleteSubscription
        sendTranslatedMessageRes = deps.sendTranslatedMessageRes
        initChatStorage = deps.Request.Persistence.initChatStorage
        initPrenotamiRequestStorage = deps.Request.Persistence.ItalianStorage.initPrenotamiRequestStorage
    }
