module internal EA.Worker.Dependencies.Embassies.Russian

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Embassies.Russian
open EA.Worker.Dependencies
open EA.Telegram.Dependencies.Producer
open Worker.Domain

module Kdmid =
    open Infrastructure.Logging
    open EA.Telegram
    open EA.Embassies.Russian.Kdmid.Dependencies

    type Dependencies =
        { getRequests: Graph.NodeId -> Async<Result<Request list, Error'>>
          pickOrder: Request list -> Async<Result<Request, Error' list>> }

        static member create (task: WorkerTask) cfg ct =
            let result = ResultBuilder()

            result {
                let! persistenceDeps = Persistence.Dependencies.create cfg
                let! producerDeps = EA.Worker.Dependencies.Telegram.Producer.create cfg ct
                let! requestDeps = Request.Dependencies.create () producerDeps
                let! kdmidDeps = Embassies.Russian.Kdmid.Dependencies.create requestDeps

                let notify notification =
                    kdmidDeps
                    |> Services.Producer.Embassies.Russian.Service.Kdmid.sendNotification notification
                    |> ResultAsync.mapError (_.Message >> Log.critical)
                    |> Async.Ignore

                let getRequests embassyId =
                    persistenceDeps.RequestStorage
                    |> Request.Query.findManyByEmbassyId embassyId
                    |> ResultAsync.map (
                        List.filter (fun request ->
                            request.SubscriptionState = Auto
                            && (request.ProcessState <> InProcess
                                || (request.ProcessState = InProcess
                                    && request.Modified < DateTime.UtcNow.Subtract(task.Duration))))
                    )

                let pickOrder requests =
                    (persistenceDeps.RequestStorage, ct)
                    ||> Order.Dependencies.create
                    |> API.Order.Kdmid.pick requests notify

                return
                    { getRequests = getRequests
                      pickOrder = pickOrder }
            }
