module internal EA.Worker.Embassies.Italian

open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Dependencies.Embassies.Italian

let private SERVICE_ID = Embassies.ITA |> Tree.NodeIdValue

module private Prenotami =
    open EA.Italian.Services.Domain.Prenotami

    let private processGroup requests =
        fun (deps: Prenotami.Dependencies) ->
            deps.tryProcessFirst requests
            |> ResultAsync.map (fun request ->
                match request.Payload.State with
                | NoAppointments -> deps.TaskName + " No appointments found." |> Log.dbg
                | HasAppointments appointments ->
                    deps.TaskName + $" Appointments found: %i{appointments.Count}" |> Log.scs)

    let start =
        fun (deps: Prenotami.Dependencies) ->
            let inline processGroup requests = deps |> processGroup requests

            deps.getRequests (SERVICE_ID |> Service.ServiceId)
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

    module SearchAppointments =
        let private handle (task, cfg, ct) =
            Prenotami.Dependencies.create task cfg ct
            |> ResultAsync.wrap start

        let Handler = {
            Id = "SA" |> Tree.NodeIdValue
            Handler = handle |> Some
        }

let createHandlers (parentTaskId: Tree.NodeId) =
    fun tasksTree ->

        let taskId = Tree.NodeId.combine [ parentTaskId; SERVICE_ID ]
        let taskHandlers = [ Prenotami.SearchAppointments.Handler ]

        tasksTree |> Worker.Client.createHandlers taskId taskHandlers
