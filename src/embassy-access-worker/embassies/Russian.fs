module internal EA.Worker.Embassies.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Domain
open EA.Worker.Dependencies.Embassies.Russian

let private createEmbassyId (task: ActiveTask) =
    task.Id.TryTakeRange 1 None
    |> Option.map (fun range -> range[.. range.Length - 2]) // Reduce the handler Id here (e.g. "RUS.SRB.BEG.SA" -> "RUS.SRB.BEG")
    |> Option.map (fun range -> (Constants.EMBASSY_ROOT_ID |> Graph.NodeIdValue) :: range)
    |> Option.map (Graph.NodeId.combine >> Ok)
    |> Option.defaultValue (
        Error
        <| Operation {
            Message = "Creating embassy Id failed."
            Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
        }
    )

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
                    | Unsuccessfully(_, error) -> error.Message |> Log.crt
                    | HasAppointments(_, appointments) ->
                        $"{deps.TaskName} Appointments found: {appointments.Count}." |> Log.scs
                    | HasConfirmations(_, confirmations) ->
                        $"{deps.TaskName} Confirmations found: {confirmations.Count}." |> Log.scs
                | None -> $"{deps.TaskName} No notifications created." |> Log.wrn)

    let start embassyId =
        fun (deps: Kdmid.Dependencies) ->
            let inline processGroup requests = deps |> processGroup requests

            deps.getRequests embassyId
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
        [<Literal>]
        let ID = "SA"

        let handle (task, cfg, ct) =
            let result = ResultBuilder()

            result {
                let! deps = Kdmid.Dependencies.create task cfg ct
                let! embassyId = task |> createEmbassyId
                return start embassyId deps
            }
            |> ResultAsync.wrap id

let private SearchAppointmentsHandler = {
    Id = Kdmid.SearchAppointments.ID |> Graph.NodeIdValue
    Handler = Kdmid.SearchAppointments.handle |> Some
}

let createHandlers (parentHandler: WorkerTaskHandler) =
    fun workerGraph ->

        let nodeId = Graph.NodeId.combine [ parentHandler.Id; "RUS" |> Graph.NodeIdValue ]
        let handlers = [ SearchAppointmentsHandler ]

        workerGraph |> Worker.Client.createHandlers nodeId handlers
