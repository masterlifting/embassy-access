module internal EA.Worker.Embassies.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Domain
open EA.Worker.Dependencies

let private createEmbassyName (task: WorkerTask) =
    try
        task.Name
        |> Graph.split
        |> List.skip 1
        |> List.take 3
        |> Graph.combine
        |> Ok
    with ex ->
        Error
        <| Operation
            { Message = $"Create embassy name failed. Error: {ex |> Exception.toMessage}"
              Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
    |> async.Return

module private Kdmid =

    let createOrder =
        fun (deps: Russian.Kdmid.Dependencies) ->
            let start getRequests =
                deps.RequestStorage
                |> getRequests
                |> ResultAsync.mapError (fun error -> [ error ])
                |> ResultAsync.bindAsync deps.pickOrder
                |> ResultAsync.mapError Error'.combine
                |> ResultAsync.map (fun request -> request.ProcessState |> string |> Info)

            start

    module SearchAppointments =
        open EA.Core.DataAccess

        [<Literal>]
        let NAME = "Search appointments"

        let setRouteNode city =
            Graph.Node(Name city, [ Graph.Node(Name NAME, []) ])

        let run (task: WorkerTask, cfg, ct) =
            Persistence.Dependencies.create cfg
            |> Result.bind (Russian.Kdmid.Dependencies.create task.Schedule ct)
            |> Result.map createOrder
            |> ResultAsync.wrap (fun startOrder ->
                task
                |> createEmbassyName
                |> ResultAsync.bindAsync (Request.Query.findManyByEmbassyName >> startOrder))

let private ROUTER =
    Graph.Node(
        Name "Russian",
        [ Graph.Node(Name "Serbia", [ "Belgrade" |> Kdmid.SearchAppointments.setRouteNode ])
          Graph.Node(Name "Germany", [ "Berlin" |> Kdmid.SearchAppointments.setRouteNode ])
          Graph.Node(Name "France", [ "Paris" |> Kdmid.SearchAppointments.setRouteNode ])
          Graph.Node(Name "Montenegro", [ "Podgorica" |> Kdmid.SearchAppointments.setRouteNode ])
          Graph.Node(Name "Ireland", [ "Dublin" |> Kdmid.SearchAppointments.setRouteNode ])
          Graph.Node(Name "Italy", [ "Rome" |> Kdmid.SearchAppointments.setRouteNode ])
          Graph.Node(Name "Switzerland", [ "Bern" |> Kdmid.SearchAppointments.setRouteNode ])
          Graph.Node(Name "Finland", [ "Helsinki" |> Kdmid.SearchAppointments.setRouteNode ])
          Graph.Node(Name "Netherlands", [ "Hague" |> Kdmid.SearchAppointments.setRouteNode ])
          Graph.Node(Name "Albania", [ "Tirana" |> Kdmid.SearchAppointments.setRouteNode ])
          Graph.Node(Name "Slovenia", [ "Ljubljana" |> Kdmid.SearchAppointments.setRouteNode ])
          Graph.Node(Name "Bosnia", [ "Sarajevo" |> Kdmid.SearchAppointments.setRouteNode ])
          Graph.Node(Name "Hungary", [ "Budapest" |> Kdmid.SearchAppointments.setRouteNode ]) ]
    )

let register () =
    ROUTER
    |> RouteNode.register (Kdmid.SearchAppointments.NAME, Kdmid.SearchAppointments.run)
