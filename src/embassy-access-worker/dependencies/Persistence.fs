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
                    do! requestStorage |> Request.Command.deleteMany requestIdsToRemove
                    
                    return
                        requestStorage
                        |> Request.Query.findManyByIds existingData
                        |> ResultAsync.map (Seq.filter (fun request ->
                            request.SubscriptionState = Auto
                            && request.ProcessState = InProcess
                            && request.Modified < System.DateTime.UtcNow.AddMinutes -5.0 ))
                        |> ResultAsync.map (Seq.map (fun request -> { request with ProcessState = Ready }))
                        |> ResultAsync.bindAsync (fun requests -> requestStorage |> Request.Command.updateMany requests)
                }

            return
                { initChatStorage = initChatStorage
                  initRequestStorage = initRequestStorage
                  resetData = resetData }
        }
