[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Russian.Midpass

open System.Threading
open Infrastructure.Domain
open Web.Clients.Domain
open EA.Telegram.Dependencies.Services.Russian

type Dependencies = {
    CT: CancellationToken
    ChatId: Telegram.ChatId
    MessageId: int
} with

    static member create(deps: Russian.Dependencies) = {
        CT = deps.CT
        ChatId = deps.Chat.Id
        MessageId = deps.MessageId
    }
