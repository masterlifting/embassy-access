[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Embassies.Russian.Midpass

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open EA.Telegram.Dependencies.Consumer.Embassies

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      CancellationToken: CancellationToken
      sendResult: Async<Result<Producer.Data, Error'>> -> Async<Result<unit, Error'>> }

    static member create(deps: Russian.Dependencies) =
        let result = ResultBuilder()

        result {
            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken
                  sendResult = deps.sendResult }
        }
