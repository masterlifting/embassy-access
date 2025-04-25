[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Russian.Russian

open System.Threading
open Infrastructure.Domain
open Web.Clients.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Services
open EA.Russian.Services.Domain

type Dependencies = {
    ct: CancellationToken
    Chat: Chat
    MessageId: int
    tryFindServiceNode: ServiceId -> Async<Result<Graph.Node<Service> option, Error'>>
    tryFindEmbassyNode: EmbassyId -> Async<Result<Graph.Node<Embassy> option, Error'>>
    initKdmidRequestStorage: unit -> Result<Request.Storage<Kdmid.Payload>, Error'>
    sendTranslatedMessageRes: Async<Result<Telegram.Producer.Message, Error'>> -> Async<Result<unit, Error'>>
} with

    static member create(deps: Services.Dependencies) = {
        ct = deps.ct
        Chat = deps.Chat
        MessageId = deps.MessageId
        tryFindServiceNode = deps.tryFindServiceNode
        tryFindEmbassyNode = deps.tryFindEmbassyNode
        sendTranslatedMessageRes = deps.sendTranslatedMessageRes
        initKdmidRequestStorage = deps.Request.Persistence.RussianStorage.initKdmidRequestStorage
    }
