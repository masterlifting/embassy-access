module internal EA.Worker.Embassies.Russian

open EA.Embassies.Russian
open Infrastructure
open Worker.Domain
open EA.Core.Domain

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

        let run country =
            fun (cfg, schedule, ct) ->
                createPickOrder cfg schedule ct
                |> ResultAsync.wrap (fun startOrder ->
                    country
                    |> Russian
                    |> EA.Persistence.Query.Request.SearchAppointments
                    |> startOrder)

let addTasks country =
    Graph.Node(
        { Name = "Russian"; Task = None },
        [ Graph.Node(
              { Name = "Search appointments"
                Task = Some <| Kdmid.SearchAppointments.run country },
              []
          )
          Graph.Node(
              { Name = "Make appointments"
                Task = None },
              []
          ) ]
    )
