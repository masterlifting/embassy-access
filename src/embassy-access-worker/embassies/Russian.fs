module internal EA.Worker.Embassies.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Domain
open EA.Embassies.Russian

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
    open EA.Embassies.Russian.Kdmid.Domain

    let private createPickOrder configuration (schedule: Schedule) ct =
        let result = ResultBuilder()

        result {
            let! filePath = configuration |> Persistence.Storage.getConnectionString "FileSystem"

            let initRequestStorage () =
                filePath
                |> EA.Core.DataAccess.Request.FileSystem
                |> EA.Core.DataAccess.Request.init

            let initChatStorage () =
                filePath
                |> EA.Telegram.DataAccess.Chat.FileSystem
                |> EA.Telegram.DataAccess.Chat.init

            let notificationDeps: EA.Telegram.Dependencies.Producer.Dependencies =
                { initChatStorage = initChatStorage
                  initRequestStorage = initRequestStorage }

            let notify notification =
                notification
                |> EA.Telegram.Producer.Produce.notification notificationDeps ct
                |> Async.map ignore

            let! requestStorage = initRequestStorage ()

            let pickOrder requests =
                let deps = Dependencies.create requestStorage ct
                let timeZone = schedule.TimeZone |> float

                let order =
                    { StartOrders = requests |> List.map (StartOrder.create timeZone)
                      notify = notify }

                order |> API.Order.Kdmid.pick deps

            let start dataAccessRequestQuery =
                requestStorage
                |> dataAccessRequestQuery
                |> ResultAsync.mapError (fun error -> [ error ])
                |> ResultAsync.bindAsync pickOrder
                |> ResultAsync.mapError Error'.combine
                |> ResultAsync.map (fun request -> request.ProcessState |> string |> Info)

            return start
        }

    module SearchAppointments =
        [<Literal>]
        let NAME = "Search appointments"

        let setRoute city =
            Graph.Node(Name city, [ Graph.Node(Name NAME, []) ])

        let run (task: WorkerTask, cfg, ct) =
            createPickOrder cfg task.Schedule ct
            |> ResultAsync.wrap (fun startOrder ->
                task
                |> createEmbassyName
                |> ResultAsync.bindAsync (EA.Core.DataAccess.Request.Query.findManyByEmbassyName >> startOrder))

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
    |> RouteGraph.register (Kdmid.SearchAppointments.NAME, Kdmid.SearchAppointments.run)
