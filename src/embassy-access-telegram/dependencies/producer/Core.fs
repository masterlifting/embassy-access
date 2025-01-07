[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Producer.Core

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies

type Dependencies =
    { CancellationToken: CancellationToken
      getEmbassyRequests: Graph.NodeId -> Async<Result<Request list, Error'>>
      getEmbassyChats: RequestId seq -> Async<Result<Chat list, Error'>>
      getSubscriptionChats: RequestId -> Async<Result<Chat list, Error'>> }

    static member create ct (deps: Persistence.Dependencies) =
        let result = ResultBuilder()

        result {

            let getEmbassyRequests (embassyId: Graph.NodeId) =
                deps.initRequestStorage ()
                |> ResultAsync.wrap (Request.Query.findManyByEmbassyId embassyId.Value)

            let getEmbassyChats (requestIds: RequestId seq) =
                deps.initChatStorage ()
                |> ResultAsync.wrap (Chat.Query.findManyBySubscriptions requestIds)

            let getSubscriptionChats requestId =
                deps.initChatStorage ()
                |> ResultAsync.wrap (Chat.Query.findManyBySubscription requestId)

            return
                { CancellationToken = ct
                  getEmbassyRequests = getEmbassyRequests
                  getEmbassyChats = getEmbassyChats
                  getSubscriptionChats = getSubscriptionChats }
        }
