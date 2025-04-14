module internal EA.Worker.Embassies.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Dependencies.Embassies.Russian

let private ServiceId = Embassies.RUS |> Graph.NodeIdValue

module private Kdmid =

    let private processGroup requests =
        fun (deps: Kdmid.Dependencies) ->
            deps.tryProcessFirst requests
            |> ResultAsync.map (fun request ->
                let errorFilter _ = true

                match request |> Notification.tryCreate errorFilter with
                | Some notification ->
                    match notification with
                    | Empty(_, message) -> $"{deps.TaskName} {message}." |> Log.dbg
                    | Unsuccessfully(_, error) -> deps.TaskName + " " + error.Message |> Log.crt
                    | HasAppointments(_, appointments) ->
                        $"{deps.TaskName} Appointments found: {appointments.Count}." |> Log.scs
                    | HasConfirmations(_, confirmations) ->
                        $"{deps.TaskName} Confirmations found: {confirmations.Count}." |> Log.scs
                | None -> $"{deps.TaskName} No notifications created." |> Log.wrn)

    let start =
        fun (deps: Kdmid.Dependencies) ->
            let inline processGroup requests = deps |> processGroup requests

            deps.getRequests ServiceId
            |> ResultAsync.map (fun requests ->
                requests
                |> Seq.groupBy (fun r -> r.Service.Id, r.Service.Payload)
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
                        |> Seq.concat
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
