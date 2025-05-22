module internal EA.Worker.Dependencies.Embassies.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Russian.Services
open EA.Russian.Services.Router
open EA.Telegram.Services.Services.Russian
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
                    
                let getRequests embassyIs serviceId = requestStorage |> Common.getRequests embassyIs serviceId
                
                let updateRequests requests =
                    requestStorage
                    |> Storage.Request.Command.updateSeq requests

                let spreadTranslatedMessages data =
                    (telegram.Culture.translateSeq, telegram.Web.Telegram.sendMessages)
                    |> Common.spreadTranslatedMessages data

                let handleProcessResult (result: Result<Request<Payload>, Error'>) =
                    result
                    |> ResultAsync.wrap (fun r ->
                        Kdmid.Command.handleProcessResult r {
                            getRequestChats = getRequestChats
                            getRequests = getRequests
                            updateRequests = updateRequests
                            spreadTranslatedMessages = spreadTranslatedMessages
                        })
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
                        | Passport Passport.Status -> false

                    serviceId |> Router.parse |> Result.exists isRequiredService

                let getRequestsToProcess rootServiceId =
                    (requestStorage, hasRequiredService)
                    |> Common.getRequestsToProcess rootServiceId task.Duration
                    |> ResultAsync.map (
                        List.filter (fun request ->
                            match request.Payload.State with
                            | NoAppointments
                            | HasAppointments _ -> true
                            | HasConfirmation _ -> false)
                    )

                let tryProcessFirst requests =

                    Kdmid.Client.init {
                        ct = ct
                        RequestStorage = requestStorage
                    }
                    |> Result.map (fun client -> client, handleProcessResult)
                    |> ResultAsync.wrap (Kdmid.Service.tryProcessFirst requests)

                return {
                    TaskName = taskName
                    getRequests = getRequestsToProcess
                    tryProcessFirst = tryProcessFirst
                }
            }
