module internal EA.Worker.Embassies.Russian

open System
open System.Threading
open Infrastructure
open Persistence.Domain
open Worker.Domain
open EA.Domain
open EA.Worker
open EA.Embassies.Russian.Domain

type private Deps =
    { Config: ProcessRequestConfiguration
      Storage: Storage.Type
      notify: Notification -> Async<Result<unit, Error'>>
      ct: CancellationToken }

let private createDeps ct configuration country =
    let deps = ModelBuilder()

    let country = country |> EA.Mapper.Country.toExternal
    let scheduleTaskName = $"{Settings.AppName}.{country.Name}.{country.City.Name}"

    deps {

        let! timeShift = scheduleTaskName |> Settings.getSchedule configuration |> Result.map _.TimeShift
        let! storage = configuration |> EA.Persistence.Storage.FileSystem.Request.create
        let notify = EA.Telegram.Producer.Produce.notification configuration ct

        let processConfig = { TimeShift = timeShift }

        return
            { Config = processConfig
              Storage = storage
              notify = notify
              ct = ct }
    }

let private processRequest deps createNotification (request: Request) =

    let processRequest deps =
        (deps.Storage, deps.Config, deps.ct)
        |> EA.Deps.Russian.processRequest
        |> EA.Api.processRequest

    let sendNotification request =
        createNotification request
        |> Option.map deps.notify
        |> Option.defaultValue (Ok() |> async.Return)
        |> ResultAsync.map (fun _ -> request)

    let sendError error =
        match error with
        | Operation reason ->
            match reason.Code with
            | Some ErrorCodes.ConfirmationExists
            | Some ErrorCodes.NotConfirmed
            | Some ErrorCodes.RequestDeleted -> Fail(request.Id, error) |> deps.notify |> Async.map (fun _ -> error)
            | _ -> error |> async.Return
        | _ -> error |> async.Return

    processRequest deps request
    |> ResultAsync.bindAsync sendNotification
    |> ResultAsync.map (fun request -> request.ProcessState |> string)
    |> ResultAsync.mapErrorAsync sendError

let private run getRequests processRequests country =
    fun (cfg, ct) ->

        // define
        let getRequests deps = getRequests deps country

        let processRequests deps =
            ResultAsync.bindAsync (processRequests deps)

        let run = ResultAsync.wrap (fun deps -> getRequests deps |> processRequests deps)

        // run

        country |> createDeps ct cfg |> run

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

    let private getRequests deps country =
        let query = Russian country |> EA.Persistence.Query.Request.SearchAppointments

        deps.Storage |> EA.Persistence.Repository.Query.Request.getMany query deps.ct

    let private processRequests deps requests =

        let createNotification = EA.Notification.Create.appointments

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
                            | 1 -> Error errors[0]
                            | _ ->
                                let msg =
                                    errors
                                    |> List.mapi (fun i error -> $"%i{i + 1}.%s{error}")
                                    |> String.concat Environment.NewLine

                                Error $"Multiple errors:%s{Environment.NewLine}%s{msg}"
                    | request :: requestsTail ->
                        match! request |> processRequest with
                        | Error error -> return! choose (errors @ [ error.MessageEx ]) requestsTail
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
        let query = Russian country |> EA.Persistence.Query.Request.MakeAppointments

        deps.Storage |> EA.Persistence.Repository.Query.Request.getMany query deps.ct

    let private processRequests deps requests =

        let createNotification = EA.Notification.Create.confirmations

        let processRequest = processRequest deps createNotification

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
