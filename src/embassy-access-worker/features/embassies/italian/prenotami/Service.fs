module internal EA.Worker.Features.Embassies.Italian.Prenotami.Service

open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Italian
open EA.Italian.Router
open EA.Italian.Domain.Prenotami
open EA.Worker.Shared
open EA.Worker.Features.Embassies.Italian.Prenotami.Infra

type private Dependencies = {
    TaskName: string
    tryProcessFirst: Request<Payload> seq -> Async<unit>
    getRequests: ServiceId -> Async<Result<Request<Payload> list, Error'>>
    cleanupResources: unit option -> Result<unit, Error'>
} with

    static member create task (deps: Worker.Task.Dependencies) ct =
        let result = ResultBuilder()
        let taskName = ActiveTask.print task

        result {
            let! requestStorage = RequestStorage.init deps.Persistence.ConnectionString deps.Persistence.EncryptionKey

            let handleProcessResult (result: Result<Request<Payload>, Error'>) =
                result
                |> ResultAsync.wrap (fun r ->
                    match r.Payload.State with
                    | NoAppointments msg -> taskName + msg |> Log.wrn
                    | HasAppointments appointments ->
                        taskName + $"Appointments found: %i{appointments.Count}" |> Log.scs
                    |> Ok
                    |> async.Return)
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

            let getRequests serviceId =
                (requestStorage, hasRequiredService)
                |> Embassies.getRequests serviceId task.Duration

            let prenotamiClient =
                Prenotami.Client.init {
                    ct = ct
                    BrowserWebApiUrl = Configuration.ENVIRONMENTS.BrowserWebApiUrl
                    RequestStorage = requestStorage
                }

            let tryProcessFirst requests =
                (prenotamiClient, handleProcessResult)
                |> Prenotami.Service.tryProcessFirst requests

            let cleanResources _ =
                requestStorage |> RequestStorage.dispose

            return {
                TaskName = taskName
                getRequests = getRequests
                tryProcessFirst = tryProcessFirst
                cleanupResources = cleanResources
            }
        }

let private start =
    fun (deps: Dependencies) ->

        [ Services.ROOT_ID; Embassies.ITA ]
        |> ServiceId.combine
        |> deps.getRequests
        |> ResultAsync.map (fun requests ->
            requests
            |> Seq.groupBy _.Service.Id
            |> Seq.map (fun (_, requests) ->
                requests
                |> Seq.sortByDescending _.Modified
                |> Seq.truncate 5
                |> deps.tryProcessFirst))
        |> ResultAsync.map Async.Sequential
        |> Async.bind (function
            | Error error -> Error error |> async.Return
            | Ok results -> results |> Async.map (fun _ -> Ok()))
        |> ResultAsync.apply deps.cleanupResources

let searchAppointments (task, deps, ct) =
    Dependencies.create task deps ct |> ResultAsync.wrap start
