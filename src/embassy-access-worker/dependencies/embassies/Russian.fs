module internal EA.Worker.Dependencies.Embassies.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Services.Services.Russian
open EA.Telegram.Dependencies.Services.Russian
open EA.Russian.Services
open EA.Russian.Services.Router
open EA.Worker.Dependencies
open EA.Worker.Dependencies.Embassies

module Kdmid =
    open EA.Russian.Services.Domain.Kdmid

    type Dependencies = {
        TaskName: string
        tryProcessFirst: Request<Payload> seq -> Async<Result<Request<Payload>, Error'>>
        getRequests: ServiceId -> Async<Result<Request<Payload> list, Error'>>
    } with

        static member create (task: ActiveTask) cfg ct =
            let result = ResultBuilder()
            let taskName = ActiveTask.print task + " "

            result {
                let! persistence = Persistence.Dependencies.create cfg
                let! telegram = Telegram.Dependencies.create cfg ct

                let! chatStorage = persistence.initChatStorage ()
                let! requestStorage = persistence.RussianStorage.initKdmidRequestStorage ()

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

                let notificationDeps: Kdmid.Notification.Dependencies = {
                    getRequestChats = getRequestChats
                    setRequestsAppointments = setRequestsAppointments
                    spreadTranslatedMessages = spreadTranslatedMessages
                }

                let notify (requestRes: Result<Request<Payload>, Error'>) =
                    requestRes
                    |> ResultAsync.wrap (fun r -> notificationDeps |> Kdmid.Notification.spread r)
                    |> ResultAsync.mapError (fun error -> taskName + error.Message |> Log.crt)
                    |> Async.Ignore

                let hasRequiredService serviceId =
                    let isRequiredService =
                        function
                        | Passport(Passport.International op)
                        | Notary(Notary.PowerOfAttorney op)
                        | Citizenship(Citizenship.Renunciation op) ->
                            match op with
                            | Operation.SlotsAutoNotification
                            | Operation.AutoBookingFirstSlot
                            | Operation.AutoBookingFirstSlotInPeriod
                            | Operation.AutoBookingLastSlot -> true
                            | Operation.CheckSlotsNow -> false
                        | Passport(Passport.Status) -> false

                    serviceId |> Router.parse |> Result.exists isRequiredService

                let getRequests rootServiceId =
                    (requestStorage, hasRequiredService)
                    |> Common.getRequests rootServiceId task.Duration

                let tryProcessFirst requests =

                    Kdmid.Client.init {
                        ct = ct
                        RequestStorage = requestStorage
                    }
                    |> Result.map (fun client -> client, notify)
                    |> ResultAsync.wrap (Kdmid.Service.tryProcessFirst requests)

                return {
                    TaskName = taskName
                    getRequests = getRequests
                    tryProcessFirst = tryProcessFirst
                }
            }
