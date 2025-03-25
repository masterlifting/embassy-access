[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Embassies.Russian.Midpass

open System.Threading
open Infrastructure.Domain
open Web.Telegram.Domain
open EA.Telegram.Domain
open EA.Telegram.Dependencies

type Dependencies =
    { Chat: Chat
      MessageId: int
      CancellationToken: CancellationToken
      Culture: Culture.Dependencies
      sendMessageRes: Async<Result<Producer.Message, Error'>> -> Async<Result<unit, Error'>> }

    static member create(deps: Russian.Dependencies) =
        { Chat = deps.Chat
          MessageId = deps.MessageId
          CancellationToken = deps.CancellationToken
          Culture = deps.Culture
          sendMessageRes = deps.sendMessageRes }
