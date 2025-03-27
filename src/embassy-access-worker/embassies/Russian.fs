module internal EA.Worker.Embassies.Russian

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Domain
open EA.Worker.Dependencies.Embassies.Russian

let private createEmbassyId (task: WorkerTask) =
    task.Id.TryTakeRange 1 None
    |> Option.map (fun range -> range[.. range.Length - 2]) // Reduce the handler Id here (e.g. "RUS.SRB.BEG.SA" -> "RUS.SRB.BEG")
    |> Option.map (fun range -> (Constants.EMBASSY_ROOT_ID |> Graph.NodeIdValue) :: range)
    |> Option.map (Graph.Node.Id.combine >> Ok)
    |> Option.defaultValue (
        Error
        <| Operation
            { Message = "Creating embassy Id failed."
              Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
    )

module private Kdmid =

    let private handleGroup pickOrder requests =
        requests
        |> pickOrder
        |> ResultAsync.mapError Error'.combine
        |> ResultAsync.bind (fun request ->
            let errorFilter _ = true

            match request |> Notification.tryCreate errorFilter with
            | Some notification ->
                match notification with
                | Empty(_, message) -> $"{message} for '{request.Service.Name}'." |> Ok
                | Unsuccessfully(_, error) -> error |> Error
                | HasAppointments(_, appointments) ->
                    $"Appointments found: {appointments.Count} for '{request.Service.Name}'." |> Ok
                | HasConfirmations(_, confirmations) ->
                    $"Confirmations found: {confirmations.Count} for '{request.Service.Name}'."
                    |> Ok
            | None -> $"No notifications created for '{request.Service.Name}'." |> Ok)

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
            |> ResultAsync.map (Async.Sequential >> Async.map Result.unzip)
            |> Async.bind (function
                | Error error -> Error error |> async.Return
                | Ok value ->
                    value
                    |> Async.map (fun (messages, errors) ->
                        let validResults =
                            messages
                            |> List.map (fun message -> $" - {message}")
                            |> String.concat Environment.NewLine
                            |> function
                                | AP.IsString x -> Environment.NewLine + x |> Some
                                | _ -> None

                        let invalidResults =
                            errors
                            |> List.map (fun error -> $" - {error.MessageOnly}")
                            |> String.concat Environment.NewLine
                            |> function
                                | AP.IsString x -> Environment.NewLine + x |> Some
                                | _ -> None

                        match validResults, invalidResults with
                        | Some validResults, Some invalidResults ->
                            $"{Environment.NewLine}Valid results:{validResults}{Environment.NewLine}Invalid results:{invalidResults}"
                            |> Warn
                            |> Ok
                        | Some validResults, None -> validResults |> Info |> Ok
                        | None, Some invalidResults ->
                            { Message = invalidResults
                              Code = None }
                            |> Operation
                            |> Error
                        | None, None -> "No results found." |> Debug |> Ok))

    module SearchAppointments =
        let HandlerId = "SA" |> Graph.NodeIdValue

        let createRouterNode cityId =
            Graph.Node(Id(cityId |> Graph.NodeIdValue), [ Graph.Node(Id HandlerId, []) ])

        let start (task, cfg, ct) =
            let result = ResultBuilder()

            result {
                let! deps = Kdmid.Dependencies.create task cfg ct
                let! embassyId = task |> createEmbassyId
                return startOrder embassyId deps
            }
            |> ResultAsync.wrap id

let private Router =

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
    Router
    |> RouteNode.register (Kdmid.SearchAppointments.HandlerId, Kdmid.SearchAppointments.start)
