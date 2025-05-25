module internal EA.Worker.Dependencies.Embassies.Italian

open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Italian.Services
open EA.Italian.Services.Router
open EA.Telegram.DataAccess
open EA.Telegram.Services.Services.Italian
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
            let taskName = ActiveTask.print task + " "

            result {

                let! persistence = Persistence.Dependencies.create cfg
                let! telegram = Telegram.Dependencies.create cfg ct

                let! chatStorage = persistence.initChatStorage ()
                let! requestStorage = persistence.ItalianStorage.initPrenotamiRequestStorage ()

                let getChats subscriptions =
                    chatStorage |> Storage.Chat.Query.findManyBySubscriptions subscriptions

                let getRequests embassyIs serviceId =
                    requestStorage |> Common.getRequests embassyIs serviceId

                let updateRequests requests =
                    requestStorage |> Storage.Request.Command.updateSeq requests

                let spreadTranslatedMessages data =
                    (telegram.Culture.translateSeq, telegram.Web.Telegram.sendMessages)
                    |> Common.spreadTranslatedMessages data

                let handleProcessResult (result: Result<Request<Payload>, Error'>) =
                    result
                    |> ResultAsync.wrap (fun r ->
                        Prenotami.Command.handleProcessResult r {
                            getChats = getChats
                            getRequests = getRequests
                            updateRequests = updateRequests
                            spreadTranslatedMessages = spreadTranslatedMessages
                        })
                    |> ResultAsync.mapError (fun error -> taskName + error.Message |> Log.crt)
                    |> Async.Ignore

                let hasRequiredService serviceId =
                    let isRequiredService =
                        function
                        | Visa(Visa.Tourism1 op)
                        | Visa(Visa.Tourism2 op) ->
                            match op with
                            | Prenotami.Operation.AutoNotifications -> true
                            | Prenotami.Operation.ManualRequest -> false

                    serviceId |> Router.parse |> Result.exists isRequiredService

                let getRequestsToProcess rootServiceId =
                    (requestStorage, hasRequiredService)
                    |> Common.getRequestsToProcess rootServiceId task.Duration

                let tryProcessFirst requests =
                    telegram.Web.initBrowser ()
                    |> ResultAsync.bindAsync (fun browser ->
                        Prenotami.Client.init {
                            ct = ct
                            RequestStorage = requestStorage
                            WebBrowser = browser
                        }
                        |> fun client -> client, handleProcessResult
                        |> Prenotami.Service.tryProcessFirst requests)

                return {
                    TaskName = taskName
                    getRequests = getRequestsToProcess
                    tryProcessFirst = tryProcessFirst
                }
            }
