module internal EA.Embassies.Russian.Kdmid.Background


open System
open System.Threading
open Infrastructure
open Persistence.Domain
open Worker.Domain
open EA.Core.Domain
open EA.Embassies.Russian.Kdmid.Domain

let private processRequest ct deps createNotification notify (request: Request) =

    let processRequest deps =
        (deps.Storage, deps.Config, ct)
        |> EA.Deps.Russian.processRequest
        |> EA.Api.processRequest

    let sendNotification request =
        createNotification request
        |> Option.map notify
        |> Option.defaultValue (Ok() |> async.Return)
        |> ResultAsync.map (fun _ -> request)

    let sendError error =
        match error with
        | Operation reason ->
            match reason.Code with
            | Some Constants.ErrorCodes.CONFIRMATION_EXISTS
            | Some Constants.ErrorCodes.NOT_CONFIRMED
            | Some Constants.ErrorCodes.REQUEST_DELETED ->
                Fail(request.Id, error) |> deps.notify |> Async.map (fun _ -> error)
            | _ -> error |> async.Return
        | _ -> error |> async.Return

    processRequest deps request
    |> ResultAsync.bindAsync sendNotification
    |> ResultAsync.map (fun request -> request.ProcessState |> string)
    |> ResultAsync.mapErrorAsync sendError

let private run getRequests processRequests notify country =
    fun (cfg, ct) ->

        // define
        let getRequests deps = getRequests ct deps country

        let processRequests ct deps notify =
            ResultAsync.bindAsync (processRequests ct deps notify)

        let run = ResultAsync.wrap (fun deps -> getRequests deps |> processRequests ct deps notify)

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

    let private getRequests ct deps country =
        let query = Russian country |> EA.Persistence.Query.Request.SearchAppointments

        deps.Storage |> EA.Persistence.Repository.Query.Request.getMany query ct

    let private processRequests ct deps notify requests =

        let createNotification = EA.Notification.Create.appointments

        let processRequest = processRequest ct deps createNotification notify

        let groupedRequests =
            requests
            |> Seq.groupBy _.Service.Name
            |> Map
            |> Map.map (fun _ requests -> requests |> Seq.truncate 5)

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

    let run notify = run getRequests processRequests notify

module private MakeAppointments =

    let private getRequests ct deps country =
        let query = Russian country |> EA.Persistence.Query.Request.MakeAppointments

        deps.Storage |> EA.Persistence.Repository.Query.Request.getMany query ct

    let private processRequests ct deps notify requests =

        let createNotification = EA.Notification.Create.confirmations

        let processRequest = processRequest ct deps createNotification notify

        async {
            let! results = requests |> Seq.map processRequest |> Async.Sequential
            return results |> toTaskResult
        }

    let run notify = run getRequests processRequests notify
