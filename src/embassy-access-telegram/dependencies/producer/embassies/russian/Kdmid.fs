module EA.Telegram.Dependencies.Producer.Embassies.Russian.Kdmid

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain.Chat
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies.Producer

type Dependencies =
    { Culture: Culture.Dependencies
      getRequestChats: Request -> Async<Result<Chat list, Error'>>
      sendNotifications: Message seq -> Async<Result<unit, Error'>> }

    static member create(deps: Request.Dependencies) =
        let result = ResultBuilder()

        result {
            let getRequestChats (request: Request) =
                deps.RequestStorage
                |> Request.Query.findManyByServiceId request.Service.Id
                |> ResultAsync.map (Seq.map _.Id)
                |> ResultAsync.bindAsync (fun subscriptionIds ->
                    deps.ChatStorage |> Chat.Query.findManyBySubscriptions subscriptionIds)

            return
                { Culture = deps.Culture
                  getRequestChats = getRequestChats
                  sendNotifications = deps.sendMessages }
        }
