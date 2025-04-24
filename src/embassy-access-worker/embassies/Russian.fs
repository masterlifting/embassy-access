module internal EA.Worker.Embassies.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Dependencies.Embassies.Russian

let private ServiceId = Embassies.RUS |> Graph.NodeIdValue

module private Kdmid =
    open EA.Russian.Services.Domain.Kdmid

    let private processGroup requests =
        fun (deps: Kdmid.Dependencies) ->
            deps.tryProcessFirst requests
            |> ResultAsync.map (fun request ->
                match request.Payload.State with
                | NoAppointments -> deps.TaskName + " No appointments found." |> Log.dbg
                | HasAppointments appointments ->
                    deps.TaskName + $" Appointments found: %i{appointments.Count}" |> Log.scs
                | HasConfirmation(msg, appointment) ->
                    deps.TaskName
                    + $" Confirmation found: %s{msg}. %s{Appointment.print appointment}"
                    |> Log.scs)

    let start =
        fun (deps: Kdmid.Dependencies) ->
            let inline processGroup requests = deps |> processGroup requests

            deps.getRequests (ServiceId |> Service.ServiceId)
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
                        errors
                        |> Seq.iter (fun error -> deps.TaskName + " Error: " + error.Message |> Log.crt)
                        |> Ok))

    module SearchAppointments =
        let private handle (task, cfg, ct) =
            Kdmid.Dependencies.create task cfg ct |> ResultAsync.wrap start

        let Handler = {
            Id = "SA" |> Graph.NodeIdValue
            Handler = handle |> Some
        }

let createHandlers (parentHandler: WorkerTaskHandler) =
    fun workerGraph ->

        let taskId = Graph.NodeId.combine [ parentHandler.Id; ServiceId ]
        let taskHandlers = [ Kdmid.SearchAppointments.Handler ]

        workerGraph |> Worker.Client.createHandlers taskId taskHandlers
