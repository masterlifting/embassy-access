module internal EA.Worker.Embassies.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Domain
open EA.Worker.Dependencies

let private createEmbassyName (task: WorkerTask) =
    try
        task.Name |> Graph.split |> List.skip 1 |> List.take 3 |> Graph.combine |> Ok
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
                |> ResultAsync.bindAsync (Request.Query.findManyByEmbassyId >> startOrder))

let private ROUTER =

    let inline createNode country city =
        Graph.Node(Name country, [ city |> Kdmid.SearchAppointments.setRouteNode ])

    Graph.Node(
        Name "Russian",
        [ createNode "Serbia" "Belgrade"
          createNode "Germany" "Berlin"
          createNode "France" "Paris"
          createNode "Montenegro" "Podgorica"
          createNode "Ireland" "Dublin"
          createNode "Italy" "Rome"
          createNode "Switzerland" "Bern"
          createNode "Finland" "Helsinki"
          createNode "Netherlands" "Hague"
          createNode "Albania" "Tirana"
          createNode "Slovenia" "Ljubljana"
          createNode "Bosnia" "Sarajevo"
          createNode "Hungary" "Budapest" ]
    )

let register () =
    ROUTER
    |> RouteNode.register (Kdmid.SearchAppointments.NAME, Kdmid.SearchAppointments.run)
