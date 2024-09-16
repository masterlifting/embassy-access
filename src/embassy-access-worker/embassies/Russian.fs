module internal EmbassyAccess.Worker.Embassies.Russian

open System
open System.Threading
open Infrastructure
open Persistence.Domain
open Worker.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Embassies.Russian.Domain
open EmbassyAccess.Worker.Notifications

type private Deps =
    { Config: ProcessRequestConfiguration
      Storage: Persistence.Storage.Type
      sendNotification: CancellationToken -> Notification -> Async<Result<int, Error'>> option
      ct: CancellationToken }

let private createDeps ct (schedule: Schedule option) =
    let deps = ModelBuilder()

    deps {
        let config =
            { TimeShift = schedule |> Option.map _.TimeShift |> Option.defaultValue 0y }

        let! storage = Persistence.Storage.create InMemory

        return
            { Config = config
              Storage = storage
              sendNotification = Telegram.send
              ct = ct }
    }

let private processRequest deps createNotification request =

    let processRequest deps =
        (deps.Storage, deps.Config, deps.ct)
        |> EmbassyAccess.Deps.Russian.processRequest
        |> EmbassyAccess.Api.processRequest

    let sendNotification notification =
        deps.sendNotification deps.ct notification
        |> Option.map (fun _ -> request)
        |> Option.defaultValue request

    processRequest deps request
    |> ResultAsync.map createNotification
    |> ResultAsync.map sendNotification
    |> ResultAsync.map (fun request -> request.State |> string)

let private run getRequests processRequests country =
    fun (_, schedule, ct) ->

        // define
        let getRequests deps = getRequests deps country

        let processRequests deps =
            ResultAsync.bind' (processRequests deps)

        let run = ResultAsync.wrap (fun deps -> getRequests deps |> processRequests deps)

        // run
        createDeps ct schedule |> run

let private toTaskResult (results: Result<string, Error'> array) =
    let messages, errors = results |> Result.unzip

    match messages, errors with
    | [], [] -> Ok <| Debug "No results."
    | [], errors ->

        Error
        <| Operation
            { Message =
                Environment.NewLine
                + (errors |> List.map _.Message |> String.concat Environment.NewLine)
              Code = None }
    | messages, [] ->

        Ok <| Info(messages |> String.concat Environment.NewLine)

    | messages, errors ->

        Ok
        <| Warn(
            Environment.NewLine
            + (messages @ (errors |> List.map _.Message) |> String.concat Environment.NewLine)
        )

module private SearchAppointments =

    let private getRequests deps country =
        let filter =
            Russian country |> EmbassyAccess.Persistence.Filter.Request.SearchAppointments

        deps.Storage
        |> EmbassyAccess.Persistence.Repository.Query.Request.get deps.ct filter

    let private processRequests deps requests =

        let processRequest = processRequest deps SendAppointments

        let uniqueRequests = requests |> Seq.filter _.GroupBy.IsNone

        let groupedRequests =
            requests
            |> Seq.filter _.GroupBy.IsSome
            |> Seq.groupBy _.GroupBy.Value
            |> Map
            |> Map.map (fun _ requests -> requests |> Seq.truncate 5)

        let groupedRequests =
            match uniqueRequests |> Seq.isEmpty with
            | true -> groupedRequests
            | false -> groupedRequests |> Map.add "Unique" uniqueRequests

        let processGroupedRequests groups =

            let rec choose (errors: string list) requests =
                async {
                    match requests with
                    | [] ->
                        return
                            match errors.Length with
                            | 0 -> Ok None
                            | 1 -> Error errors[0]
                            | _ ->
                                let msg =
                                    errors
                                    |> List.mapi (fun i error -> $"%i{i + 1}.%s{error}")
                                    |> String.concat Environment.NewLine

                                Error $"Multiple errors:%s{Environment.NewLine}%s{msg}"

                    | request :: requestsTail ->
                        match! request |> processRequest with
                        | Error error -> return! choose (errors @ [ error.Message ]) requestsTail
                        | Ok result -> return Ok <| Some result
                }

            let processGroup key group =
                group
                |> Seq.toList
                |> choose []
                |> ResultAsync.map (fun result ->
                    match result with
                    | Some result -> $"'%s{key}': %s{result}"
                    | None -> $"'%s{key}'. No results.")
                |> ResultAsync.mapError (fun error -> $"'{key}': %s{error}")

            groups |> Map.map processGroup |> Map.values |> Async.Parallel

        async {
            let! results = groupedRequests |> processGroupedRequests

            return
                results
                |> Array.map (Result.mapError (fun error -> Operation { Message = error; Code = None }))
                |> toTaskResult
        }

    let run = run getRequests processRequests

module private MakeAppointments =

    let private getRequests deps country =
        let filter =
            Russian country |> EmbassyAccess.Persistence.Filter.Request.MakeAppointments

        deps.Storage
        |> EmbassyAccess.Persistence.Repository.Query.Request.get deps.ct filter

    let private processRequests deps requests =

        let processRequest = processRequest deps SendConfirmations

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
