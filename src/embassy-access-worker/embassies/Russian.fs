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
        |> ResultAsync.map (function
            | Some request -> request.ProcessState.Message
            | None -> "No requests found to handle.")

    let startOrder embassyId =
        fun (deps: Kdmid.Dependencies) ->
            deps.getRequests embassyId
            |> ResultAsync.map (fun requests ->
                requests
                |> Seq.groupBy _.Service.Id.Value
                |> Seq.map (fun (_, requests) ->
                    requests |> Seq.truncate 5 |> Seq.toList |> handleGroup deps.pickOrder))
            |> ResultAsync.map (Async.Parallel >> Async.map Result.unzip)
            |> Async.map (function
                | Error error -> Error error |> async.Return
                | Ok value ->
                    value
                    |> Async.map (fun (messages, errors) ->
                        let messages =
                            messages
                            |> List.map (fun message -> $"Message: {message}")
                            |> String.concat ", "

                        let errors =
                            errors
                            |> List.map _.Message
                            |> List.mapi (fun i message -> $"{i + 1}. {message}")
                            |> String.concat Environment.NewLine

                        let result =
                            if System.String.IsNullOrWhiteSpace errors then
                                messages
                            else
                                $"{messages}, {errors}"

                        result |> Info |> Ok))

    module SearchAppointments =
        let ID = "SA" |> Graph.NodeIdValue

        let createRouterNode cityId =
            Graph.Node(Id(cityId |> Graph.NodeIdValue), [ Graph.Node(Id ID, []) ])

        let start (task, cfg, ct) =
            let result = ResultBuilder()

            result {
                let! persistenceDeps = Persistence.Dependencies.create cfg
                let! webDeps = Web.Dependencies.create ()
                let! deps = Kdmid.Dependencies.create ct persistenceDeps webDeps
                let! embassyId = task |> createEmbassyId
                return startOrder embassyId deps
            }
            |> ResultAsync.wrap id

let private ROUTER =

    let inline createNode countryId cityId =
        Graph.Node(Id(countryId |> Graph.NodeIdValue), [ cityId |> Kdmid.SearchAppointments.createRouterNode ])

    Graph.Node(
        Id("RU" |> Graph.NodeIdValue),
        [ createNode "SRB" "BG"
          createNode "GER" "BER"
          createNode "FRA" "PAR"
          createNode "MNE" "PDG"
          createNode "IRL" "DUB"
          createNode "SWI" "BER"
          createNode "FIN" "HEL"
          createNode "NLD" "HAG"
          createNode "ALB" "TIR"
          createNode "SLO" "LJU"
          createNode "BIH" "SAR"
          createNode "HUN" "BUD" ]
    )

let register () =
    ROUTER
    |> RouteNode.register (Kdmid.SearchAppointments.ID, Kdmid.SearchAppointments.start)
