module internal EA.Worker.Embassies.Russian

open Infrastructure
open Worker.Domain
open EA.Core.Domain
open EA.Embassies.Russian.Kdmid

type private Dependencies =
    { searchAppointments: EA.Persistence.Query.Request.GetMany -> Async<Result<WorkerTaskResult, Error'>> }

let private createDependencies configuration schedule ct =
    let deps = ResultBuilder()

    deps {
        let! storage = configuration |> EA.Persistence.Storage.FileSystem.Request.create

        let notify notification =
            notification
            |> EA.Telegram.Producer.Produce.notification configuration ct
            |> Async.map ignore

        let pickRequest requests =
            let deps = Request.createDependencies ct storage
            let timeZone = schedule.TimeZone |> float

            requests
            |> Seq.map (fun request -> timeZone, request)
            |> Request.pick deps notify

        let processData query =
            storage
            |> EA.Persistence.Repository.Query.Request.getMany query ct
            |> ResultAsync.mapError (fun error -> [ error ])
            |> ResultAsync.bindAsync pickRequest
            |> ResultAsync.mapError List.head
            |> ResultAsync.map (fun request -> request.ProcessState |> string |> Info)

        return { searchAppointments = processData }
    }

module private SearchAppointments =

    let run country =
        fun (cfg, schedule, ct) ->
            createDependencies cfg schedule ct
            |> ResultAsync.wrap (fun deps ->
                country
                |> Russian
                |> EA.Persistence.Query.Request.SearchAppointments
                |> deps.searchAppointments)

let addTasks country =
    Graph.Node(
        { Name = "Russian"; Task = None },
        [ Graph.Node(
              { Name = "Search appointments"
                Task = Some <| SearchAppointments.run country },
              []
          )
          Graph.Node(
              { Name = "Make appointments"
                Task = None },
              []
          ) ]
    )
