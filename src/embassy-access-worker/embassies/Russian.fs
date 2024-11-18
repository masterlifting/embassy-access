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
      getRequests: unit -> Async<Result<Request list, Error'>>
      notify: Notification -> Async<Result<unit, Error'>> }

let private createDependencies configuration schedule ct query  country =
    let deps = ResultBuilder()

    let country = country |> EA.Core.Mapper.Country.toExternal

    deps {

        let timeZone = schedule.TimeZone |> float
        let! storage = configuration |> EA.Persistence.Storage.FileSystem.Request.create
        let notify = EA.Telegram.Producer.Produce.notification configuration ct

        let kdmidDeps = Domain.Dependencies.create ct storage

        let pickRequest =
            fun requests -> requests |> Seq.map (fun request -> timeZone, request) |> Request.pick kdmidDeps

        let getRequests () =
            storage |> EA.Persistence.Repository.Query.Request.getMany query ct

        return
            { processRequest = Request.start kdmidDeps timeZone
              pickRequest = pickRequest
              getRequests = getRequests
              notify = notify }
    }

let private processRequest deps (request: Request) =

    let sendNotification request =
        Notification.tryCreate Request.errorFilter request
        |> Option.map deps.notify
        |> Option.defaultValue (Ok() |> async.Return)
        |> ResultAsync.map (fun _ -> request)

    let sendError error =
        match error |> Request.errorFilter with
        | false -> error |> async.Return
        | true -> Fail(request.Id, error) |> deps.notify |> Async.map (fun _ -> error)

    deps.processRequest request
    |> ResultAsync.bindAsync sendNotification
    |> ResultAsync.map (fun request -> request.ProcessState |> string)
    |> ResultAsync.mapErrorAsync sendError

let private run getRequests query country =
    fun (cfg, schedule, ct) ->

        // define
        let getRequests deps = getRequests deps country

        let processRequests deps =
            ResultAsync.bindAsync (processRequests deps)

        let run = ResultAsync.wrap (fun deps -> getRequests deps |> processRequests deps)

        // run

        country |> createDependencies cfg schedule, ct |> run

let private toTaskResult (results: Result<string, Error'> array) =
    let messages, errors = results |> Result.unzip

    match messages, errors with
    | [], [] -> Ok <| Debug "No results."
    | [], errors ->

        Error
        <| Operation
            { Message =
                Environment.NewLine
                + (errors |> List.map _.MessageEx |> String.concat Environment.NewLine)
              Code = None }
    | messages, [] ->

        Ok <| Info(messages |> String.concat Environment.NewLine)

    | messages, errors ->

        Ok
        <| Warn(
            Environment.NewLine
            + (messages @ (errors |> List.map _.MessageEx) |> String.concat Environment.NewLine)
        )

module private SearchAppointments =

    let private processRequests deps requests =

        let requestsGroups =
            requests
            |> Seq.groupBy (fun request -> $"{request.Service.Embassy.Country.City} - {request.Service.Name}")
            |> Map
            |> Map.map (fun _ requests -> requests |> Seq.truncate 5)

        let processGroup key requests =
            requests
            |> deps.pickRequest
            |> ResultAsync.mapError (fun errors ->
                Operation
                    { Message =
                        key
                        + Environment.NewLine
                        + (errors |> List.map _.MessageEx |> String.concat Environment.NewLine)
                      Code = None })

        requestsGroups |> Map.map processGroup |> Map.values |> Async.Parallel

    let run country =
        fun (cfg, schedule, ct) ->
            let query = Russian country |> EA.Persistence.Query.Request.SearchAppointments
            createDependencies cfg schedule ct query country
            |> ResultAsync.bind(fun deps -> deps.

module private MakeAppointments =

    let private getRequests deps country =
        let query = Russian country |> EA.Persistence.Query.Request.MakeAppointments

        deps.Storage |> EA.Persistence.Repository.Query.Request.getMany query deps.ct

    let private processRequests deps requests =

        let processRequest = processRequest deps

        async {
            let! results = requests |> Seq.map processRequest |> Async.Sequential
            return results |> toTaskResult
        }

    let run = run getRequests processRequests

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
