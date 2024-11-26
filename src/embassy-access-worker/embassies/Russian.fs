module internal EA.Worker.Embassies.Russian

open EA.Embassies.Russian
open Infrastructure
open Worker.Domain
open EA.Core.Domain

type WorkerRoute =
    | Name of string

    interface Graph.INodeName with
        member this.Id = Graph.NodeId.New

        member this.Name =
            match this with
            | Name name -> name

        member this.setName name = Name name

let private WORKER_ROUTER =
    Graph.Node(
        Name "Russian",
        [ Graph.Node(Name "Serbia", [ Graph.Node(Name "Belgrade", []) ])
          Graph.Node(Name "Germany", [ Graph.Node(Name "Berlin", []) ])
          Graph.Node(Name "France", [ Graph.Node(Name "Paris", []) ])
          Graph.Node(Name "Montenegro", [ Graph.Node(Name "Podgorica", []) ])
          Graph.Node(Name "Ireland", [ Graph.Node(Name "Dublin", []) ])
          Graph.Node(Name "Switzerland", [ Graph.Node(Name "Bern", []) ])
          Graph.Node(Name "Finland", [ Graph.Node(Name "Helsinki", []) ])
          Graph.Node(Name "Netherlands", [ Graph.Node(Name "Hague", []) ])
          Graph.Node(Name "Albania", [ Graph.Node(Name "Tirana", []) ])
          Graph.Node(Name "Slovenia", [ Graph.Node(Name "Ljubljana", []) ])
          Graph.Node(Name "Bosnia", [ Graph.Node(Name "Sarajevo", []) ])
          Graph.Node(Name "Hungary", [ Graph.Node(Name "Budapest", []) ]) ]
    )

module internal Kdmid =
    open EA.Embassies.Russian.Kdmid.Domain

    let private createPickOrder configuration (schedule: WorkerSchedule) ct =
        let startOrder = ResultBuilder()

        startOrder {
            let! storage = configuration |> EA.Persistence.Storage.FileSystem.Request.create

            let notify notification =
                notification
                |> EA.Telegram.Producer.Produce.notification configuration ct
                |> Async.map ignore

            let pickOrder requests =
                let deps = Dependencies.create ct storage
                let timeZone = schedule.TimeZone |> float

                let order =
                    { StartOrders = requests |> List.map (StartOrder.create timeZone)
                      notify = notify }

                order |> API.Order.Kdmid.pick deps

            let start query =
                storage
                |> EA.Persistence.Repository.Query.Request.getMany query ct
                |> ResultAsync.mapError (fun error -> [ error ])
                |> ResultAsync.bindAsync pickOrder
                |> ResultAsync.mapError List.head
                |> ResultAsync.map (fun request -> request.ProcessState |> string |> Info)

            return start
        }

    module SearchAppointments =

        let run () =
            fun (task: WorkerTaskOut, cfg, ct) ->
                createPickOrder cfg task.Schedule ct
                |> ResultAsync.wrap (fun startOrder ->
                    Belgrade
                    |> Serbia
                    |> Russian
                    |> EA.Persistence.Query.Request.SearchAppointments
                    |> startOrder)

let register () =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Russian"
          Task = None },
        [ Graph.Node(
              { Id = Graph.NodeId.New
                Name = "Search appointments"
                Task = Some <| Kdmid.SearchAppointments.run () },
              []
          )
          Graph.Node(
              { Id = Graph.NodeId.New
                Name = "Make appointments"
                Task = None },
              []
          ) ]
    )
    
let register'() =
    let rec innerLoop (node: Graph.Node<WorkerRoute>) =
        let handler =
            match node.Value with
            | Name "Search appointments" -> Kdmid.SearchAppointments.run ()
    
    WORKER_ROUTER |> innerLoop
