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
        |> Graph.splitNodeName
        |> List.skip 1
        |> List.take 3
        |> Graph.buildNodeNameOfSeq
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

        let setRoute city =
            Graph.Node(Name city, [ Graph.Node(Name NAME, []) ])

        let run (task: WorkerTask, cfg, ct) =
            Russian.Kdmid.Dependencies.create task.Schedule cfg ct
            |> Result.map createOrder
            |> ResultAsync.wrap (fun startOrder ->
                task
                |> createEmbassyName
                |> ResultAsync.bindAsync (Request.Query.findManyByEmbassyName >> startOrder))

let private ROUTER =
    Graph.Node(
        Name "Russian",
        [ Graph.Node(Name "Serbia", [ "Belgrade" |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name "Germany", [ "Berlin" |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name "France", [ "Paris" |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name "Montenegro", [ "Podgorica" |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name "Ireland", [ "Dublin" |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name "Italy", [ "Rome" |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name "Switzerland", [ "Bern" |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name "Finland", [ "Helsinki" |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name "Netherlands", [ "Hague" |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name "Albania", [ "Tirana" |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name "Slovenia", [ "Ljubljana" |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name "Bosnia", [ "Sarajevo" |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name "Hungary", [ "Budapest" |> Kdmid.SearchAppointments.setRoute ]) ]
    )

let register () =
    ROUTER
    |> RouteNode.register (Kdmid.SearchAppointments.NAME, Kdmid.SearchAppointments.run)
