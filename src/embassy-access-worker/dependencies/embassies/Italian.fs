module internal EA.Worker.Dependencies.Embassies.Italian

open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Telegram.Services.Embassies
open EA.Telegram.Dependencies
open EA.Worker.Dependencies
open EA.Italian.Services

module Prenotami =
    open EA.Italian.Services.Domain.Prenotami

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

                let notificationDeps: Notification.Dependencies = {
                    printPayload = Payload.create >> Result.map Payload.print
                    translateMessages = telegram.Culture.translateSeq
                    setRequestAppointments = telegram.Persistence.setRequestAppointments
                    getRequestChats = telegram.Persistence.getRequestChats
                    sendMessages = telegram.Web.Telegram.sendMessages
                }

                let notify notification =
                    notificationDeps
                    |> Notification.spread notification
                    |> ResultAsync.mapError (_.Message >> Log.crt)
                    |> Async.Ignore

                let getRequests partServiceId =
                    persistence.getRequests (partServiceId, task)

                let tryProcessFirst requests =
                    {
                        CancellationToken = ct
                        RequestStorage = persistence.RequestStorage
                    }
                    |> Prenotami.Client.init
                    |> Result.map (fun client -> client, notify)
                    |> ResultAsync.wrap (Prenotami.Service.tryProcessFirst requests)

                return {
                    TaskName = ActiveTask.print task
                    getRequests = getRequests
                    tryProcessFirst = tryProcessFirst
                }
            }
