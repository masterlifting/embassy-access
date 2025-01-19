module internal EA.Worker.Embassies.Russian

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
    |> async.Return

module private Kdmid =

    let createOrder =
        fun (deps: Kdmid.Dependencies) ->
            let start getRequests =
                deps.RequestStorage
                |> getRequests
                |> ResultAsync.mapError (fun error -> [ error ])
                |> ResultAsync.bindAsync deps.pickOrder
                |> ResultAsync.mapError Error'.combine
                |> ResultAsync.map (function
                    | Some request -> request.ProcessState |> string |> Info
                    | None -> "No requests found to handle." |> Trace)

            start

    module SearchAppointments =
        open EA.Core.DataAccess

        let ID = "SA" |> Graph.NodeIdValue

        let setRouteNode cityId =
            Graph.Node(Id(cityId |> Graph.NodeIdValue), [ Graph.Node(Id ID, []) ])

        let handle (task: WorkerTask, cfg, ct) =
            Persistence.Dependencies.create cfg
            |> Result.bind (fun persistenceDeps ->
                Web.Dependencies.create ()
                |> Result.map (fun webDeps -> (persistenceDeps, webDeps)))
            |> Result.bind (fun (persistenceDeps, webDeps) -> Kdmid.Dependencies.create ct persistenceDeps webDeps)
            |> Result.map createOrder
            |> ResultAsync.wrap (fun startOrder ->
                task
                |> createEmbassyId
                |> ResultAsync.bindAsync (Request.Query.findManyByEmbassyId >> startOrder))

let private ROUTER =

    let inline createNode countryId cityId =
        Graph.Node(Id(countryId |> Graph.NodeIdValue), [ cityId |> Kdmid.SearchAppointments.setRouteNode ])

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
    |> RouteNode.register (Kdmid.SearchAppointments.ID, Kdmid.SearchAppointments.handle)
