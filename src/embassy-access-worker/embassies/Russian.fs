module internal EmbassyAccess.Worker.Embassies.Russian

open System
open System.Threading
open Infrastructure
open Persistence.Domain
open Worker.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Worker
open EmbassyAccess.Embassies.Russian.Domain

type private Deps =
    { Config: ProcessRequestConfiguration
      Storage: Persistence.Storage.Type
      sendNotification: Notification -> Async<Result<unit, Error'>>
      ct: CancellationToken }

let private createDeps ct configuration taskName =
    let deps = ModelBuilder()

    deps {

        let! timeShift = taskName |> Settings.getSchedule configuration |> Result.map _.TimeShift

        let! storage = Persistence.Storage.create InMemory

        let sendNotification =
            EmbassyAccess.Telegram.Producer.Produce.notification ct
            >> ResultAsync.map (fun _ -> ())

        return
            { Config = { TimeShift = timeShift }
              Storage = storage
              sendNotification = sendNotification
              ct = ct }
    }

let private processRequest deps createNotification (request: Request) =

    let processRequest deps =
        (deps.Storage, deps.Config, deps.ct)
        |> EmbassyAccess.Deps.Russian.processRequest
        |> EmbassyAccess.Api.processRequest

    let sendNotification request =
        createNotification request
        |> Option.map deps.sendNotification
        |> Option.defaultValue (Ok() |> async.Return)
        |> ResultAsync.map (fun _ -> request)

    let sendError error =
        match error with
        | Operation reason ->
            match reason.Code with
            | Some ErrorCodes.ConfirmationExists
            | Some ErrorCodes.NotConfirmed
            | Some ErrorCodes.RequestDeleted ->
                Notification.Error(request.Id, error)
                |> deps.sendNotification
                |> Async.map (fun _ -> error)
            | _ -> error |> async.Return
        | _ -> error |> async.Return

    processRequest deps request
    |> ResultAsync.bindAsync sendNotification
    |> ResultAsync.map (fun request -> request.State |> string)
    |> ResultAsync.mapErrorAsync sendError

let private run taskName getRequests processRequests country =
    fun (cfg, ct) ->

        // define
        let getRequests deps = getRequests deps country

        let processRequests deps =
            ResultAsync.bindAsync (processRequests deps)

        let run = ResultAsync.wrap (fun deps -> getRequests deps |> processRequests deps)

        // run
        taskName |> createDeps ct cfg |> run

let private toTaskResult (results: Result<string, Error'> array) =
    let messages, errors = results |> Result.unzip

    match messages, errors with
    | [], [] -> Ok <| Debug "No results."
    | [], errors ->

        Result.Error
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

    [<Literal>]
    let Name = "Search appointments"

    let private getRequests deps country =
        let filter =
            Russian country |> EmbassyAccess.Persistence.Filter.Request.SearchAppointments

        deps.Storage
        |> EmbassyAccess.Persistence.Repository.Query.Request.get deps.ct filter

    let private processRequests deps requests =

        let createNotification = Notification.Create.searchAppointments

        let processRequest = processRequest deps createNotification

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
                            | 1 -> Result.Error errors[0]
                            | _ ->
                                let msg =
                                    errors
                                    |> List.mapi (fun i error -> $"%i{i + 1}.%s{error}")
                                    |> String.concat Environment.NewLine

                                Result.Error $"Multiple errors:%s{Environment.NewLine}%s{msg}"

                    | request :: requestsTail ->
                        match! request |> processRequest with
                        | Result.Error error -> return! choose (errors @ [ error.Message ]) requestsTail
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

    let run = run Name getRequests processRequests

module private MakeAppointments =

    [<Literal>]
    let Name = "Make appointments"

    let private getRequests deps country =
        let filter =
            Russian country |> EmbassyAccess.Persistence.Filter.Request.MakeAppointments

        deps.Storage
        |> EmbassyAccess.Persistence.Repository.Query.Request.get deps.ct filter

    let private processRequests deps requests =

        let createNotification = Notification.Create.makeConfirmations

        let processRequest = processRequest deps createNotification

        async {
            let! results = requests |> Seq.map processRequest |> Async.Sequential
            return results |> toTaskResult
        }

    let run = run Name getRequests processRequests

let addTasks country =
    Graph.Node(
        { Name = "Russian"; Task = None },
        [ Graph.Node(
              { Name = SearchAppointments.Name
                Task = Some <| SearchAppointments.run country },
              []
          )
          Graph.Node(
              { Name = MakeAppointments.Name
                Task = Some <| MakeAppointments.run country },
              []
          ) ]
    )
