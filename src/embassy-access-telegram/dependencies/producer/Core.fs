[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Producer.Core

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess

type Dependencies =
    { getEmbassyRequests: Graph.NodeId -> Async<Result<Request list, Error'>>
      getEmbassyChats: RequestId seq -> Async<Result<Chat list, Error'>> }

    static member create(deps: EA.Telegram.Dependencies.Persistence.Dependencies) =
        let result = ResultBuilder()

        result {

            let getEmbassyRequests (embassyId: Graph.NodeId) =
                deps.initRequestStorage ()
                |> ResultAsync.wrap (Request.Query.findManyByEmbassyId embassyId.Value)

            let getChats (requestIds: RequestId seq) =
                deps.initChatStorage ()
                |> ResultAsync.wrap (Chat.Query.findManyBySubscriptions requestIds)

            return
                { getEmbassyRequests = getEmbassyRequests
                  getEmbassyChats = getChats }
        }
