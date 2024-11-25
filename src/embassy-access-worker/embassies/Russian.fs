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
        { Id = "1631a3ab-31d8-438a-bd4f-effc58462ee2" |> Graph.NodeId.create
          Name = "Russian"
          Task = None },
        [ Graph.Node(
              { Id = "b1a218ba-7c36-44bf-a208-99fc89aac375" |> Graph.NodeId.create
                Name = "Search appointments"
                Task = Some <| Kdmid.SearchAppointments.run country },
              []
          )
          Graph.Node(
              { Id = "c81e9a09-54cb-4548-aeab-05dd19e4fa58" |> Graph.NodeId.create
                Name = "Make appointments"
                Task = None },
              []
          ) ]
    )
