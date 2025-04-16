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
        tryProcessFirst: Request<Payload> list -> Async<Result<Request<Payload>, Error' list>>
        getRequests: ServiceId -> Async<Result<Request<Payload> list, Error'>>
    } with

        static member create (task: ActiveTask) cfg ct =
            let result = ResultBuilder()

            result {
                let! persistence = Persistence.Dependencies.create cfg
                let! telegram = Telegram.Dependencies.create cfg ct
                
                let! requestsStorage = persistence.initItalianPrenotamiRequestsStorage()

                let notificationDeps: Notification.Dependencies = {
                    printPayload = Credentials.create >> Result.map Credentials.print
                    translateMessages = telegram.Culture.translateSeq
                    setRequestAppointments = telegram.Persistence.setRequestAppointments
                    getRequestChats = telegram.Persistence.getRequestChats
                    sendMessages = telegram.Web.Telegram.sendMessages
                }

                let notify (request: Request<Payload>) =
                    notificationDeps
                    |> Notification.spread notification
                    |> ResultAsync.mapError (_.Message >> Log.crt)
                    |> Async.Ignore

                let getRequests partServiceId =
                    persistence.getRequests (partServiceId, task)

                let tryProcessFirst requests =
                    {
                        CancellationToken = ct
                        RequestsTable = requestsStorage
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
