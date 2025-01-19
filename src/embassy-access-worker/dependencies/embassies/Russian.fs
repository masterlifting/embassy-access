module internal EA.Worker.Dependencies.Embassies.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Embassies.Russian
open EA.Worker.Dependencies

module Kdmid =
    open Infrastructure.Logging
    open EA.Telegram
    open EA.Embassies.Russian.Kdmid.Dependencies

    type Dependencies =
        { getRequests: Graph.NodeId -> Async<Result<Request list, Error'>>
          pickOrder: Request list -> Async<Result<Request option, Error' list>> }

        static member create ct (persistenceDeps: Persistence.Dependencies) (webDeps: Web.Dependencies) =
            let result = ResultBuilder()

            result {
                let! requestStorage = persistenceDeps.initRequestStorage ()

                let telegramProducerDeps: Dependencies.Producer.Producer.Dependencies =
                    { CancellationToken = ct
                      initTelegramClient = webDeps.initTelegramClient
                      initChatStorage = persistenceDeps.initChatStorage }

                let! telegramProducerKdmidDeps =
                    Dependencies.Producer.Embassies.Russian.Kdmid.Dependencies.create telegramProducerDeps

                let notify notification =
                    telegramProducerKdmidDeps
                    |> Services.Producer.Embassies.Russian.Service.Kdmid.sendNotification notification
                    |> ResultAsync.mapError (_.Message >> Log.critical)
                    |> Async.Ignore

                let getRequests embassyId =
                    requestStorage
                    |> Request.Query.findManyByEmbassyId embassyId
                    |> ResultAsync.map (List.filter (fun r -> r.SubscriptionState = Auto))

                let pickOrder requests =
                    (requestStorage, ct)
                    ||> Order.Dependencies.create
                    |> API.Order.Kdmid.pick requests notify

                return
                    { getRequests = getRequests
                      pickOrder = pickOrder }
            }
