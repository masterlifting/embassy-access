[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Russian.Russian

open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Services

type Dependencies = {
    Chat: Chat
    MessageId: int
    Request: Request.Dependencies
} with

    static member create(deps: Services.Dependencies) =
        let result = ResultBuilder()

        result {
            return {
                Chat = deps.Chat
                MessageId = deps.MessageId
                Request = deps.Request
            }
        }
