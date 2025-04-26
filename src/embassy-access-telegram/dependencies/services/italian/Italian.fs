module EA.Telegram.Dependencies.Services.Italian.Italian

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

type Dependencies = {
    ct: CancellationToken
    Chat: Chat
    MessageId: int
    tryFindServiceNode: ServiceId -> Async<Result<Graph.Node<Service> option, Error'>>
    tryFindEmbassyNode: EmbassyId -> Async<Result<Graph.Node<Embassy> option, Error'>>
    initChatStorage: unit -> Result<Chat.Storage, Error'>
    initPrenotamiRequestStorage: unit -> Result<Request.Storage<Prenotami.Payload>, Error'>
    sendTranslatedMessageRes: Async<Result<Telegram.Producer.Message, Error'>> -> Async<Result<unit, Error'>>
} with

    static member create(deps: Services.Dependencies) = {
        ct = deps.ct
        Chat = deps.Chat
        MessageId = deps.MessageId
        tryFindServiceNode = deps.tryFindServiceNode
        tryFindEmbassyNode = deps.tryFindEmbassyNode
        sendTranslatedMessageRes = deps.sendTranslatedMessageRes
        initChatStorage = deps.Request.Persistence.initChatStorage
        initPrenotamiRequestStorage = deps.Request.Persistence.ItalianStorage.initPrenotamiRequestStorage
    }
