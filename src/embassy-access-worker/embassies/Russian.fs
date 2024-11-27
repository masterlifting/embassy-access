module internal EA.Worker.Embassies.Russian

open EA.Embassies.Russian
open Infrastructure
open Worker.Domain
open EA.Worker.Domain
open EA.Core.Domain

module private Kdmid =
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
        [<Literal>]
        let NAME = "Search appointments"

        let setRoute city =
            Graph.Node(Name city, [ Graph.Node(Name NAME, []) ])

        let run (task: WorkerTaskOut, cfg, ct) =
            createPickOrder cfg task.Schedule ct
            |> ResultAsync.wrap (fun startOrder ->
                Belgrade
                |> Serbia
                |> Russian
                |> EA.Persistence.Query.Request.SearchAppointments
                |> startOrder)

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
    |> WorkerRoute.register (Kdmid.SearchAppointments.NAME, Kdmid.SearchAppointments.run)
