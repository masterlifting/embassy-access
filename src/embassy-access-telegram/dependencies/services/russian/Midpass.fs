[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Russian.Midpass

open System.Threading
open Infrastructure.Domain
open EA.Core.Domain
open Web.Clients.Domain
open EA.Telegram.Dependencies.Services.Russian

type Dependencies = {
    CT: CancellationToken
    ChatId: Telegram.ChatId
    MessageId: int
    tryFindServiceNode: ServiceId -> Async<Result<Graph.Node<Service> option, Error'>>
    tryFindEmbassyNode: EmbassyId -> Async<Result<Graph.Node<Embassy> option, Error'>>
    sendTranslatedMessageRes: Async<Result<Telegram.Producer.Message, Error'>> -> Async<Result<unit, Error'>>
} with

    static member create(deps: Russian.Dependencies) = {
        CT = deps.ct
        ChatId = deps.Chat.Id
        MessageId = deps.MessageId
        tryFindServiceNode = deps.tryFindServiceNode
        tryFindEmbassyNode = deps.tryFindEmbassyNode
        sendTranslatedMessageRes = deps.sendTranslatedMessageRes
    }
