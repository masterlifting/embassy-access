module internal EmbassyAccess.Worker.Embassies.Russian

open System
open Infrastructure
open Persistence.Domain
open Web.Domain
open Worker.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Persistence
open EmbassyAccess.Embassies.Russian.Domain

type private Deps =
    { Config: EmbassyAccess.Embassies.Russian.Domain.ProcessRequestConfiguration
      Storage: Persistence.Storage.Type
      Bot: Web.Client.Type
      ct: Threading.CancellationToken }

let private createDeps ct (schedule: Schedule option) =
    let deps = ModelBuilder()
    
    deps {
        let config =
            { TimeShift = schedule |> Option.map _.TimeShift |> Option.defaultValue 0y }

        let! storage = Persistence.Storage.create InMemory

        let! bot =
            Configuration.getEnvVar "TelegramBotToken"
            |> Result.bind (Option.map Ok >> Option.defaultValue (Error <| NotFound "Telegram bot token"))
            |> Result.map Telegram
            |> Result.bind Web.Client.create

        return
            { Config = config
              Storage = storage
              Bot = bot
              ct = ct }
    }

let private processRequest deps notify request =

    let processRequest deps =
        (deps.Storage, deps.Config, deps.ct)
        |> EmbassyAccess.Deps.Russian.processRequest
        |> EmbassyAccess.Api.processRequest

    processRequest deps request
    |> ResultAsync.bind' notify
    |> ResultAsync.map (fun request -> request.State |> string)

let private run country getRequests processRequests =
    fun (_, schedule, ct) ->

        // define
        let getRequests deps = getRequests deps country

        let processRequests deps =
            ResultAsync.bind' (processRequests deps)

        let run = ResultAsync.wrap (fun deps -> getRequests deps |> processRequests deps)

        // run
        createDeps ct schedule |> run

module private SearchAppointments =

    let private getRequests deps country =
        let filter = Russian country |> Filter.Request.SearchAppointments
        deps.Storage |> Repository.Query.Request.get deps.ct filter

    let private processRequests deps requests =

        let notifySubscribers request =
            async {
                match request.State with
                | Completed _ ->
                    match request.Appointments.IsEmpty with
                    | false ->
                        return!
                            deps.Bot
                            |> EmbassyAccess.Api.notifySubscribers request.Embassy request.Appointments
                            |> ResultAsync.map (fun _ -> request)
                    | true -> return Ok request
                | _ -> return Ok request
            }

        let processRequest = processRequest deps notifySubscribers

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

        let toWorkerResult (results: Result<string, string> array) =
            let msgs, errors = results |> Result.unzip

            match msgs, errors with
            | [], [] -> Ok <| Debug "No results."
            | [], errors ->

                Error
                <| Operation
                    { Message = Environment.NewLine + (errors |> String.concat Environment.NewLine)
                      Code = None }

            | msgs, [] ->

                Ok <| Info(msgs |> String.concat Environment.NewLine)

            | msgs, errors ->

                Ok
                <| Warn(Environment.NewLine + (msgs @ errors |> String.concat Environment.NewLine))

        async {
            let! results = groupedRequests |> processGroupedRequests
            return results |> toWorkerResult
        }

    let run country = run country getRequests processRequests

module private MakeAppointments =

    let private getRequests deps country =
        let filter = Russian country |> Filter.Request.MakeAppointments
        deps.Storage |> Repository.Query.Request.get deps.ct filter

    let private processRequests deps requests =

        let notifySubscriber request =
            async {
                match request.State with
                | Completed _ ->
                    match request.Appointments.IsEmpty with
                    | false ->
                        match request.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
                        | [] -> return Ok request
                        | confirmations ->
                            return!
                                deps.Bot
                                |> EmbassyAccess.Api.notifySubscriber request.Id confirmations
                                |> ResultAsync.map (fun _ -> request)
                    | true -> return Ok request
                | _ -> return Ok request
            }

        let processRequest = processRequest deps notifySubscriber

        let toWorkerResult (results: Result<string, Error'> array) =
            let msgs, errors = results |> Result.unzip

            match msgs, errors with
            | [], [] -> Ok <| Debug "No results."
            | [], errors ->

                Error
                <| Operation
                    { Message =
                        Environment.NewLine
                        + (errors |> List.map _.Message |> String.concat Environment.NewLine)
                      Code = None }
            | msgs, [] ->

                Ok <| Info(msgs |> String.concat Environment.NewLine)

            | msgs, errors ->

                Ok
                <| Warn(
                    Environment.NewLine
                    + (msgs @ (errors |> List.map _.Message) |> String.concat Environment.NewLine)
                )

        async {
            let! results = requests |> Seq.map processRequest |> Async.Sequential
            return results |> toWorkerResult
        }

    let run country = run country getRequests processRequests

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
