module internal EA.Worker.Dependencies.Embassies.Italian

open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Services.Services.Italian
open EA.Telegram.Dependencies.Services.Italian
open EA.Italian.Services
open EA.Worker.Dependencies
open EA.Worker.Dependencies.Embassies

module Prenotami =
    open EA.Italian.Services.Domain.Prenotami

    type Dependencies = {
        TaskName: string
        tryProcessFirst: Request<Payload> seq -> Async<Result<Request<Payload>, Error'>>
        getRequests: ServiceId -> Async<Result<Request<Payload> list, Error'>>
    } with

        static member create (task: ActiveTask) cfg ct =
            let result = ResultBuilder()

            result {
                let! persistence = Persistence.Dependencies.create cfg
                let! telegram = Telegram.Dependencies.create cfg ct

                let! chatStorage = persistence.initChatStorage ()
                let! requestStorage = persistence.ItalianStorage.initPrenotamiRequestStorage ()

                let getRequestChats request =
                    (requestStorage, chatStorage) |> Common.getRequestChats request

                let setRequestsAppointments embassyId serviceId appointments =
                    requestStorage
                    |> Storage.Request.Query.findMany (
                        Storage.Request.Query.ByEmbassyAndServiceId(embassyId, serviceId)
                    )
                    |> ResultAsync.bindAsync (fun requests ->
                        requestStorage
                        |> Storage.Request.Command.updateSeq (
                            requests
                            |> Seq.map (fun request -> {
                                request with
                                    Payload = {
                                        request.Payload with
                                            State = HasAppointments appointments
                                    }
                            })
                        ))

                let spreadTranslatedMessages data =
                    (telegram.Culture.translateSeq, telegram.Web.Telegram.sendMessages)
                    |> Common.spreadTranslatedMessages data

                let notificationDeps: Prenotami.Notification.Dependencies = {
                    getRequestChats = getRequestChats
                    setRequestsAppointments = setRequestsAppointments
                    spreadTranslatedMessages = spreadTranslatedMessages
                }

                let notify (requestRes: Result<Request<Payload>, Error'>) =
                    requestRes
                    |> ResultAsync.wrap (fun r -> notificationDeps |> Prenotami.Notification.spread r)
                    |> ResultAsync.mapError (_.Message >> Log.crt)
                    |> Async.Ignore

                let getRequests serviceId =
                    requestStorage |> Common.getRequests serviceId task

                let tryProcessFirst requests =
                    Prenotami.Client.init {
                        ct = ct
                        RequestStorage = requestStorage
                    }
                    |> ResultAsync.map (fun client -> client, notify)
                    |> ResultAsync.bindAsync (Prenotami.Service.tryProcessFirst requests)

                return {
                    TaskName = ActiveTask.print task
                    getRequests = getRequests
                    tryProcessFirst = tryProcessFirst
                }
            }
