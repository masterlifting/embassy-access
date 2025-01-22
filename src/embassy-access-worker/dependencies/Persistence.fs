[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Persistence

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess

type Dependencies =
    { initChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'>
      resetData: unit -> Async<Result<unit, Error'>> }

    static member create cfg =
        let result = ResultBuilder()

        result {
            let! connectionString = cfg |> Persistence.Storage.getConnectionString "FileSystem"

            let initChatStorage () =
                connectionString |> Chat.FileSystem |> Chat.init

            let initRequestStorage () =
                connectionString |> Request.FileSystem |> Request.init

            let resetData () =
                let resultAsync = ResultAsyncBuilder()

                resultAsync {

                    let! chatStorage = initChatStorage () |> async.Return
                    let! requestStorage = initRequestStorage () |> async.Return

                    let! subscriptions = chatStorage |> Chat.Query.getSubscriptions |> ResultAsync.map Set.ofSeq
                    let! requestIdentifiers = requestStorage |> Request.Query.getIdentifiers |> ResultAsync.map Set.ofSeq

                    let existingData = subscriptions |> Set.intersect <| requestIdentifiers
                    let subscriptionsToRemove = existingData |> Set.difference subscriptions
                    let requestIdsToRemove = existingData |> Set.difference requestIdentifiers
                    
                    do! chatStorage |> Chat.Command.deleteSubscriptions subscriptionsToRemove
                    return requestStorage |> Request.Command.deleteMany requestIdsToRemove
                }

            return
                { initChatStorage = initChatStorage
                  initRequestStorage = initRequestStorage
                  resetData = resetData }
        }
