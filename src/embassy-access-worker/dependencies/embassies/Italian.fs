module internal EA.Worker.Dependencies.Embassies.Italian

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Worker.Dependencies
open Worker.Domain
open EA.Russian.Clients
open EA.Russian.Clients.Domain.Kdmid
open Infrastructure.Logging
open EA.Telegram.Services.Embassies.Russian.Kdmid
open EA.Telegram.Dependencies.Embassies.Russian

type Dependencies = {
    TaskName: string
    getRequests: Graph.NodeId -> Async<Result<Request list, Error'>>
    tryProcessFirst: Request list -> Async<Result<Request, Error' list>>
} with

    static member create (task: ActiveTask) cfg ct =
        let result = ResultBuilder()

        result {
            let! persistence = Persistence.Dependencies.create cfg
            let! telegram = Telegram.Dependencies.create cfg ct

            let notificationDeps: Kdmid.Notification.Dependencies = {
                translateMessages = telegram.Culture.translateSeq
                setRequestAppointments = telegram.Persistence.setRequestAppointments
                getRequestChats = telegram.Persistence.getRequestChats
                sendMessages = telegram.Web.Telegram.sendMessages
            }

            let notify notification =
                notificationDeps
                |> Message.Notification.spread notification
                |> ResultAsync.mapError (_.Message >> Log.crt)
                |> Async.Ignore

            let getRequests partServiceId = persistence.getRequests (partServiceId, task)

            let tryProcessFirst requests =
                {
                    CancellationToken = ct
                    RequestStorage = persistence.RequestStorage
                }
                |> Kdmid.Client.init
                |> Result.map (fun client -> client, notify)
                |> ResultAsync.wrap (Kdmid.Service.tryProcessFirst requests)

            return {
                TaskName = ActiveTask.print task
                getRequests = getRequests
                tryProcessFirst = tryProcessFirst
            }
        }
