[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Embassies.Russian.Midpass

open System.Threading
open EA.Telegram.Domain
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Dependencies.Consumer.Embassies

type Dependencies =
    { Chat: Chat
      MessageId: int
      CancellationToken: CancellationToken
      Culture: Culture.Dependencies
      sendResult: Async<Result<Producer.Message, Error'>> -> Async<Result<unit, Error'>> }

    static member create(deps: Russian.Dependencies) =
        let result = ResultBuilder()

        result {
            return
                { Chat = deps.Chat
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken
                  Culture = deps.Culture
                  sendResult = deps.sendResult }
        }
