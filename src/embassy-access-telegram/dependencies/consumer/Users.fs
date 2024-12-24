[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Users

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Telegram.DataAccess
open EA.Core.DataAccess

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      CancellationToken: CancellationToken
      EmbassiesDeps: Embassies.Dependencies
      getUserEmbassies: ChatId -> Async<Result<EmbassyNode list, Error'>>
      getUserEmbassy: ChatId -> Graph.NodeId -> Async<Result<EmbassyNode, Error'>> }

    static member create(deps: Core.Dependencies) =
        let result = ResultBuilder()

        result {

            let! embassiesDeps = Embassies.Dependencies.create deps

            let getUserEmbassies chatId =
                deps.ChatStorage
                |> Chat.Query.tryFindById chatId
                |> ResultAsync.bindAsync (function
                    | None -> $"Telegram chat {chatId.ValueStr}" |> NotFound |> Error |> async.Return
                    | Some chat ->
                        deps.RequestStorage
                        |> Request.Query.findManyByIds chat.Subscriptions
                        |> ResultAsync.map (List.map _.Service.Embassy))

            let getUserEmbassy chatId embassyId =
                getUserEmbassies chatId
                |> ResultAsync.bind (fun embassies ->
                    embassies
                    |> List.tryFind (fun embassy -> embassy.Id = embassyId)
                    |> Option.map Ok
                    |> Option.defaultValue ($"Embassy with Id {embassyId.Value}" |> NotFound |> Error))

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken
                  EmbassiesDeps = embassiesDeps
                  getUserEmbassies = getUserEmbassies
                  getUserEmbassy = getUserEmbassy }
        }
