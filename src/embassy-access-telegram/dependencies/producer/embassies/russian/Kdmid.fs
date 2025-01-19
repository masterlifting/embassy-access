module EA.Telegram.Dependencies.Producer.Embassies.Russian.Kdmid

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain.Producer
open Web.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Domain.Chat
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Producer

type Dependencies =
    { getRequestChats: Request -> Async<Result<Chat list, Error'>>
      sendNotifications: Data seq -> Async<Result<unit, Error'>> }

    static member create(deps: Producer.Dependencies) =
        let result = ResultBuilder()

        result {
            let getRequestChats (request: Request) =
                deps.initChatStorage ()
                |> ResultAsync.wrap (Chat.Query.findManyBySubscription request.Id)

            let sendNotifications data =
                deps.initTelegramClient ()
                |> ResultAsync.wrap (produceSeq data deps.CancellationToken)
                |> ResultAsync.map ignore

            return
                { getRequestChats = getRequestChats
                  sendNotifications = sendNotifications }
        }
