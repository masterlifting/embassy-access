module internal EA.Worker.Dependencies.Embassies.Italian

open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies
open EA.Worker.Dependencies
open EA.Italian.Services
open EA.Telegram.Dependencies.Embassies.Italian

module Prenotami =
    open EA.Italian.Services.Domain.Prenotami
    open EA.Telegram.Services.Embassies.Italian.Prenotami

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
                
                let! chatStorage = persistence.initChatStorage()
                let! requestStorage = persistence.ItalianStorage.initPrenotamiRequestStorage()
                
                let getRequestChats request =
                    requestStorage
                    |> Storage.Request.Query.findManyByServiceId request.Service.Id
                    |> ResultAsync.map (Seq.map _.Id)
                    |> ResultAsync.bindAsync (fun subscriptionIds ->
                        chatStorage |> Storage.Chat.Query.findManyBySubscriptions subscriptionIds)

                let setRequestAppointments serviceId appointments =
                        requestStorage
                        |> Storage.Request.Query.findManyByServiceId serviceId
                        |> ResultAsync.map (fun requests ->
                            requests
                            |> Seq.map (fun request -> {
                                request with
                                    Request.Payload.Appointments = appointments
                            }))
                        |> ResultAsync.bindAsync (fun requests -> requestStorage |> Storage.Request.Command.updateSeq requests)
                        
                let notificationDeps: Prenotami.Notification.Dependencies = {
                    getRequestChats = getRequestChats
                    setRequestAppointments = setRequestAppointments
                    sendMessages = telegram.Web.Telegram.sendMessages
                    translateMessages = telegram.Culture.translateSeq
                }

                let notify (requestRes: Result<Request<Payload>, Error'>) =
                    requestRes
                    |> ResultAsync.wrap (fun r -> notificationDeps |> Notification.spread r)
                    |> ResultAsync.mapError (_.Message >> Log.crt)
                    |> Async.Ignore

                let getRequests serviceId =
                    requestStorage
                    |> Common.getRequests serviceId task

                let tryProcessFirst requests =
                    {
                        CancellationToken = ct
                        RequestStorage = requestStorage
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
