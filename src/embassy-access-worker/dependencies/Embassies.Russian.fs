module internal EA.Worker.Dependencies.Embassies.Russian

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Worker.Dependencies
open Worker.Domain
open EA.Russian.Clients
open EA.Russian.Clients.Domain.Kdmid

module Kdmid =
    open Infrastructure.Logging
    open EA.Telegram.Services.Embassies.Russian.Kdmid
    open EA.Telegram.Dependencies.Embassies.Russian

    type Dependencies = {
        getRequests: Graph.NodeId -> Async<Result<Request list, Error'>>
        tryProcessFirst: Request list -> Async<Result<Request, Error' list>>
    } with

        static member create (task: WorkerTask) cfg ct =
            let result = ResultBuilder()

            result {
                let! persistenceDeps = Persistence.Dependencies.create cfg
                let! telegramDeps = Telegram.Dependencies.create cfg ct

                let notificationDeps: Kdmid.Notification.Dependencies = {
                    translateMessages = telegramDeps.Culture.translateSeq
                    setRequestAppointments = telegramDeps.Persistence.setRequestAppointments
                    getRequestChats = telegramDeps.Persistence.getRequestChats
                    sendMessages = telegramDeps.Web.Telegram.sendMessages
                }

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
                            request.IsBackground
                            && (request.ProcessState <> InProcess
                                || request.ProcessState = InProcess
                                   && request.Modified < DateTime.UtcNow.Subtract task.Duration))
                    )

                let tryProcessFirst requests =
                    {
                        CancellationToken = ct
                        RequestStorage = persistenceDeps.RequestStorage
                    }
                    |> Kdmid.Client.init
                    |> Result.map (fun client -> client, notify)
                    |> ResultAsync.wrap (Kdmid.Service.tryProcessFirst requests)

                return {
                    getRequests = getRequests
                    tryProcessFirst = tryProcessFirst
                }
            }
