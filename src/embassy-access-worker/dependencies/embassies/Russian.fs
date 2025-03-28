module internal EA.Worker.Dependencies.Embassies.Russian

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Embassies.Russian
open EA.Worker.Dependencies
open Worker.Domain

module Kdmid =
    open Infrastructure.Logging
    open EA.Embassies.Russian.Kdmid.Dependencies
    open EA.Telegram.Services.Embassies.Russian.Kdmid
    open EA.Telegram.Dependencies.Embassies.Russian

    type Dependencies =
        { getRequests: Graph.NodeId -> Async<Result<Request list, Error'>>
          pickOrder: Request list -> Async<Result<Request, Error' list>> }

        static member create (task: WorkerTask) cfg ct =
            let result = ResultBuilder()

            result {
                let! persistenceDeps = Persistence.Dependencies.create cfg
                let! tgDeps = Telegram.Dependencies.create cfg ct

                let notificationDeps: Kdmid.Notification.Dependencies =
                    { translateMessages = tgDeps.Culture.translateSeq
                      setRequestAppointments = tgDeps.Persistence.setRequestAppointments
                      getRequestChats = tgDeps.Persistence.getRequestChats
                      sendMessages = tgDeps.Web.Telegram.sendMessages }

                let notify notification =
                    notificationDeps
                    |> Message.Notification.spread notification
                    |> ResultAsync.mapError (_.Message >> Log.critical)
                    |> Async.Ignore

                let getRequests embassyId =
                    persistenceDeps.RequestStorage
                    |> Request.Query.findManyByEmbassyId embassyId
                    |> ResultAsync.map (
                        List.filter (fun request ->
                            request.SubscriptionState = Auto
                            && (request.ProcessState <> InProcess
                                || request.ProcessState = InProcess
                                   && request.Modified < DateTime.UtcNow.Subtract task.Duration))
                    )

                let pickOrder requests =
                    (persistenceDeps.RequestStorage, ct)
                    ||> Order.Dependencies.create
                    |> API.Order.Kdmid.pick requests notify

                return
                    { getRequests = getRequests
                      pickOrder = pickOrder }
            }
