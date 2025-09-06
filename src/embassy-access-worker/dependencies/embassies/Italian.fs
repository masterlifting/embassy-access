module internal EA.Worker.Dependencies.Embassies.Italian

open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Italian.Services
open EA.Italian.Services.Router
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

                let! requestStorage = persistence.ItalianStorage.initPrenotamiRequestStorage ()

                let handleProcessResult (result: Result<Request<Payload>, Error'>) =
                    result
                    |> ResultAsync.wrap (fun r -> Ok() |> async.Return) //TODO: add result handling
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

                let getRequests rootServiceId =
                    (requestStorage, hasRequiredService)
                    |> Common.getRequests rootServiceId task.Duration

                let tryProcessFirst requests =
                    Prenotami.Client.init {
                        ct = ct
                        BrowserWebApiUrl = Configuration.ENVIRONMENTS.BrowserWebApiUrl
                        RequestStorage = requestStorage
                    }
                    |> fun client -> client, handleProcessResult
                    |> Prenotami.Service.tryProcessFirst requests

                return {
                    TaskName = taskName
                    getRequests = getRequests
                    tryProcessFirst = tryProcessFirst
                }
            }
