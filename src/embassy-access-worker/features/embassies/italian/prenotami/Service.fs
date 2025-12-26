module internal EA.Worker.Features.Embassies.Italian.Prenotami.Service

open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Italian.Services
open EA.Italian.Services.Router
open EA.Italian.Services.Domain.Prenotami
open EA.Worker.Shared
open EA.Worker.Features.Embassies.Italian.Prenotami.Infra

type private Dependencies = {
    TaskName: string
    tryProcessFirst: Request<Payload> seq -> Async<Result<Request<Payload>, Error'>>
    getRequests: ServiceId -> Async<Result<Request<Payload> list, Error'>>
    cleanResources: unit option -> Result<unit, Error'>
} with

    static member create task (deps: Worker.Task.Dependencies) ct =
        let result = ResultBuilder()
        let taskName = ActiveTask.print task

        result {
            let! requestStorage = RequestStorage.init deps.Persistence.ConnectionString deps.Persistence.EncryptionKey

            let handleProcessResult (result: Result<Request<Payload>, Error'>) =
                result
                |> ResultAsync.wrap (fun r -> r.Payload.State.Print() |> Log.scs |> Ok |> async.Return)
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

            let tryProcessFirst requests =
                Prenotami.Client.init {
                    ct = ct
                    BrowserWebApiUrl = Configuration.ENVIRONMENTS.BrowserWebApiUrl
                    RequestStorage = requestStorage
                }
                |> fun client -> client, handleProcessResult
                |> Prenotami.Service.tryProcessFirst requests

            let cleanResources _ =
                requestStorage |> RequestStorage.dispose

            return {
                TaskName = taskName
                getRequests = getRequests
                tryProcessFirst = tryProcessFirst
                cleanResources = cleanResources
            }
        }

let private processGroup requests =
    fun (deps: Dependencies) ->
        deps.tryProcessFirst requests
        |> ResultAsync.map (fun request ->
            match request.Payload.State with
            | NoAppointments msg -> deps.TaskName + $" {msg}." |> Log.dbg
            | HasAppointments appointments -> deps.TaskName + $" Appointments found: %i{appointments.Count}" |> Log.scs)

let private start =
    fun (deps: Dependencies) ->
        let inline processGroup requests = deps |> processGroup requests

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
                |> Seq.toList
                |> processGroup))
        |> ResultAsync.map (Async.Sequential >> Async.map Result.unzip)
        |> Async.bind (function
            | Error error -> Error error |> async.Return
            | Ok results ->
                results
                |> Async.map (fun (_, errors) ->
                    errors |> Seq.iter (fun error -> deps.TaskName + error.Message |> Log.crt) |> Ok))
        |> ResultAsync.apply deps.cleanResources

let searchAppointments (task, deps, ct) =
    Dependencies.create task deps ct |> ResultAsync.wrap start
