module internal EA.Worker.Embassies.Russian

open EA.Embassies.Russian
open Infrastructure
open Worker.Domain
open EA.Worker.Domain
open EA.Core.Domain
open EA.Core.Persistence

let private createEmbassyName (task: WorkerTaskOut) =
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
            { Message = ex |> Exception.toMessage
              Code = ErrorReason.buildLineOpt (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) }

module private Kdmid =
    open EA.Embassies.Russian.Kdmid.Domain

    let private createPickOrder configuration (schedule: WorkerSchedule) ct =
        let result = ResultBuilder()

        result {
            let! storage = configuration |> Storage.FileSystem.Request.create

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
                query
                |> Repository.Query.Request.findMany storage ct
                |> ResultAsync.mapError (fun error -> [ error ])
                |> ResultAsync.bindAsync pickOrder
                |> ResultAsync.mapError Error.ofList
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
                task
                |> createEmbassyName
                |> async.Return
                |> ResultAsync.bindAsync (Query.Request.ByEmbassyName >> startOrder))

open EA.Core.Domain.Constants

let private ROUTER =
    Graph.Node(
        Name Embassy.RUSSIAN,
        [ Graph.Node(Name Country.SERBIA, [ City.BELGRADE |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name Country.GERMANY, [ City.BERLIN |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name Country.FRANCE, [ City.PARIS |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name Country.MONTENEGRO, [ City.PODGORICA |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name Country.IRELAND, [ City.DUBLIN |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name Country.ITALY, [ City.ROME |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name Country.SWITZERLAND, [ City.BERN |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name Country.FINLAND, [ City.HELSINKI |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name Country.NETHERLANDS, [ City.HAGUE |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name Country.ALBANIA, [ City.TIRANA |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name Country.SLOVENIA, [ City.LJUBLJANA |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name Country.BOSNIA, [ City.SARAJEVO |> Kdmid.SearchAppointments.setRoute ])
          Graph.Node(Name Country.HUNGARY, [ City.BUDAPEST |> Kdmid.SearchAppointments.setRoute ]) ]
    )

let register () =
    ROUTER
    |> WorkerRoute.register (Kdmid.SearchAppointments.NAME, Kdmid.SearchAppointments.run)
