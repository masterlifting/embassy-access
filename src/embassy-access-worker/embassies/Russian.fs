module internal EA.Worker.Embassies.Russian

open System
open System.Threading
open Infrastructure
open Persistence.Domain
open Worker.Domain
open EA.Core.Domain
open EA.Worker
open EA.Embassies.Russian.Kdmid

type private Dependencies =
    { processRequest: Request -> Async<Result<Request, Error'>>
      pickRequest: Request seq -> Async<Result<Request, Error' list>>
      getRequests: EA.Persistence.Query.Request.GetMany -> Async<Result<Request list, Error'>>
      notify: Notification -> Async<Result<unit, Error'>> }

let private createDependencies configuration schedule ct =
    let deps = ResultBuilder()

    deps {

        let timeZone = schedule.TimeZone |> float
        let! storage = configuration |> EA.Persistence.Storage.FileSystem.Request.create
        let notify = EA.Telegram.Producer.Produce.notification configuration ct

        let deps = Domain.Dependencies.create ct storage

        let pickRequest =
            fun requests -> requests |> Seq.map (fun request -> timeZone, request) |> Request.pick deps

        let getRequests query =
            storage |> EA.Persistence.Repository.Query.Request.getMany query ct

        return
            { processRequest = Request.start deps timeZone
              pickRequest = pickRequest
              getRequests = getRequests
              notify = notify }
    }

module private SearchAppointments =

    let run country =
        fun (cfg, schedule, ct) ->
            createDependencies cfg schedule ct
            |> ResultAsync.wrap (fun deps ->
                country
                |> Russian
                |> EA.Persistence.Query.Request.SearchAppointments
                |> deps.getRequests
                |> ResultAsync.bindAsync (fun requests -> deps.pickRequest requests))
                // |> ResultAsync.mapError (List.head)
                // |> ResultAsync.map (fun request -> request.ProcessState |> string |> Info))

module private MakeAppointments =
    let run country =
        fun (cfg, schedule, ct) ->
            createDependencies cfg schedule ct
            |> ResultAsync.wrap (fun deps ->
                country
                |> Russian
                |> EA.Persistence.Query.Request.SearchAppointments
                |> deps.getRequests
                |> ResultAsync.bindAsync (fun requests -> deps.pickRequest requests)
                |> ResultAsync.map (fun request -> request.ProcessState |> string |> Info))
            |> ResultAsync.mapError (List.head)

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
                Task = Some <| MakeAppointments.run country },
              []
          ) ]
    )
