module EA.Telegram.Dependencies.Services.Italian.Italian

open System.Threading
open EA.Core.Domain
open EA.Telegram.Domain
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Services

type Dependencies = {
    CT: CancellationToken
    Chat: Chat
    MessageId: int
} with

    static member create(deps: Services.Dependencies) = {
        CT = deps.CT
        Chat = deps.Chat
        MessageId = deps.MessageId
    }
