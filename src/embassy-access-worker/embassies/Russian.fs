module internal EA.Worker.Embassies.Russian

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Domain
open EA.Worker.Dependencies
open EA.Worker.Dependencies.Embassies.Russian

let private createEmbassyId (task: WorkerTask) =
    try
        let value =
            task.Id.Value |> Graph.split |> List.skip 1 |> List.take 3 |> Graph.combine

        [ "EMB"; value ] |> Graph.combine |> Graph.NodeIdValue |> Ok
    with ex ->
        Error
        <| Operation
            { Message = $"Getting embassy Id failed. Error: {ex |> Exception.toMessage}"
              Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }

module private Kdmid =

    let private handleGroup pickOrder requests =
        requests
        |> pickOrder
        |> ResultAsync.mapError Error'.combine
        |> ResultAsync.bind (function
            | Some request ->
                let errorFilter _ = true

                match request |> Notification.tryCreate errorFilter with
                | Some notification ->
                    match notification with
                    | Successfully(_, message) ->
                        $"Successfully processed: {message} for '{request.Service.Name}'." |> Ok
                    | Unsuccessfully(_, error) -> error |> Error
                    | HasAppointments(_, appointments) ->
                        $"Appointments found: {appointments.Count} for '{request.Service.Name}'." |> Ok
                    | HasConfirmations(_, confirmations) ->
                        $"Confirmations found: {confirmations.Count} for '{request.Service.Name}'."
                        |> Ok
                | None -> $"No notifications created for '{request.Service.Name}'." |> Ok
            | None -> "No requests found to handle." |> Ok)

    let startOrder embassyId =
        fun (deps: Kdmid.Dependencies) ->
            deps.getRequests embassyId
            |> ResultAsync.map (fun requests ->
                requests
                |> Seq.groupBy _.Service.Id.Value
                |> Seq.map (fun (_, requests) ->
                    requests
                    |> Seq.sortByDescending _.Modified
                    |> Seq.truncate 5
                    |> Seq.toList
                    |> handleGroup deps.pickOrder))
            |> ResultAsync.map (Async.Parallel >> Async.map Result.unzip)
            |> Async.bind (function
                | Error error -> Error error |> async.Return
                | Ok value ->
                    value
                    |> Async.map (fun (messages, errors) ->
                        let messages =
                            messages
                            |> List.map (fun message -> $" - {message}")
                            |> String.concat Environment.NewLine
                            |> function
                                | AP.IsString x -> Environment.NewLine + x |> Some
                                | _ -> None

                        let errors =
                            errors
                            |> List.map (fun error -> $" - {error.MessageOnly}")
                            |> String.concat Environment.NewLine
                            |> function
                                | AP.IsString x -> Environment.NewLine + x |> Some
                                | _ -> None

                        match messages, errors with
                        | Some messages, Some errors ->
                            $"{Environment.NewLine}Valid results:{messages}{Environment.NewLine}Invalid results:{errors}"
                            |> Warn
                            |> Ok
                        | Some messages, None -> $"{Environment.NewLine}{messages}" |> box |> Success |> Ok
                        | None, Some errors -> { Message = errors; Code = None } |> Operation |> Error
                        | None, None -> "No results found." |> Debug |> Ok))

    module SearchAppointments =
        let ID = "SA" |> Graph.NodeIdValue

        let createRouterNode cityId =
            Graph.Node(Id(cityId |> Graph.NodeIdValue), [ Graph.Node(Id ID, []) ])

        let start (task, cfg, ct) =
            let result = ResultBuilder()

            result {
                let! persistenceDeps = Persistence.Dependencies.create cfg
                let! webDeps = Web.Dependencies.create ()
                let! deps = Kdmid.Dependencies.create ct task persistenceDeps webDeps
                let! embassyId = task |> createEmbassyId
                return startOrder embassyId deps
            }
            |> ResultAsync.wrap id

let private ROUTER =

    let inline createNode countryId cityId =
        Graph.Node(Id(countryId |> Graph.NodeIdValue), [ cityId |> Kdmid.SearchAppointments.createRouterNode ])

    Graph.Node(
        Id("RUS" |> Graph.NodeIdValue),
        [ createNode "SRB" "BEG"
          createNode "DEU" "BER"
          createNode "FRA" "PAR"
          createNode "MNE" "POD"
          createNode "IRL" "DUB"
          createNode "CHE" "BER"
          createNode "FIN" "HEL"
          createNode "NLD" "HAG"
          createNode "ALB" "TIA"
          createNode "SVN" "LJU"
          createNode "BIH" "SJJ"
          createNode "HUN" "BUD" ]
    )

let register () =
    ROUTER
    |> RouteNode.register (Kdmid.SearchAppointments.ID, Kdmid.SearchAppointments.start)
