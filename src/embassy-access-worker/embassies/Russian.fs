﻿module internal EmbassyAccess.Worker.Embassies.Russian

open System
open Infrastructure
open Persistence.Domain
open Worker.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Persistence
open EmbassyAccess.Embassies.Russian.Domain

let private createConfig (schedule: Schedule option) =
    { TimeShift = schedule |> Option.map _.TimeShift |> Option.defaultValue 0y }

let private processRequest ct config storage =
    (storage, config, ct)
    |> EmbassyAccess.Deps.Russian.processRequest
    |> EmbassyAccess.Api.processRequest

let private mapRequestState request =
    match request.State with
    | Failed error -> Error error
    | Completed msg -> Ok msg
    | state -> Ok $"Request {request.Id.Value} is not completed. Current state: {state}"

let private createTaskResult (results: Result<Request, Error'> array) =
    results
    |> Seq.map (function
        | Ok request ->
            match request |> mapRequestState with
            | Ok msg -> msg
            | Error error -> error.Message
        | Error error -> error.Message)
    |> Seq.foldi (fun state index msg -> $"%s{state}{Environment.NewLine}%i{index + 1}. %s{msg}") ""
    |> Info
    |> Ok

module private SearchAppointments =

    let private getRequests ct country storage =
        let filter = Russian country |> Filter.Request.SearchAppointments
        storage |> Repository.Query.Request.get ct filter

    let private processRequests ct schedule storage requests =
        let config = createConfig schedule

        let uniqueRequests = requests |> Seq.filter _.GroupBy.IsNone

        let groupedRequests =
            requests
            |> Seq.filter _.GroupBy.IsSome
            |> Seq.groupBy _.GroupBy.Value
            |> Map
            |> Map.map (fun _ requests -> requests |> Seq.take 5)
            |> Map.add "unique" uniqueRequests

        let processRequest = processRequest ct config storage

        let notify request = async { return Ok request }

        let processGroupedRequests (groups: Map<string, Request seq>) =

            let rec choose (errors: Error' list) requests =
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
                                    |> List.mapi (fun i error -> $"%i{i + 1}.%s{error.Message}")
                                    |> String.concat Environment.NewLine

                                Error
                                <| Operation
                                    { Message = $"Multiple errors: %s{Environment.NewLine}%s{msg}"
                                      Code = None }

                    | request :: requestsTail ->
                        match! request |> processRequest with
                        | Error error -> return! choose (errors @ [ error ]) requestsTail
                        | Ok result -> return Ok <| Some result
                }

            let processGroup name group =
                async {
                    let requests = group |> Seq.toList

                    match! requests |> choose [] with
                    | Ok request ->
                        match request with
                        | Some request ->
                            match! requests |> Seq.map notify |> Async.Parallel |> Async.Catch with
                            | Choice1Of2 _ -> return Ok $"Processed requests in group '%s{name}'; 
                            | Choice2Of2 ex ->
                                let message = ex |> Exception.toMessage
                                return Error <| Operation { Message = message; Code = None }
                        | None -> return Error <| NotFound $"Correctly processed requests in group '%s{name}'"
                    | Error error -> return Error error
                }

            async {
                let! results = groups |> Map.map processGroup |> Map.values |> Async.Parallel

                return
                    results
                    |> Seq.roes
                    |> Result.map (Seq.choose id)
                    |> Result.mapError (fun errors -> Operation { Message = ""; Code = None })
            }

        async {
            match! groupedRequests |> processGroupedRequests with
            | Ok requests -> return Ok <| Info ""
            | Error error -> return Error error
        }

    let run country =
        fun (_, schedule, ct) ->
            Persistence.Storage.create InMemory
            |> ResultAsync.wrap (fun storage ->
                storage
                |> getRequests ct country
                |> ResultAsync.bind' (processRequests ct schedule storage))

module private MakeAppointments =

    let private getRequests ct country storage =
        let filter = Russian country |> Filter.Request.MakeAppointments
        storage |> Repository.Query.Request.get ct filter

    let private processRequests ct schedule storage requests =
        let config = createConfig schedule

        let processRequest = processRequest ct config storage

        async {
            let! results = requests |> Seq.map processRequest |> Async.Sequential
            return results |> createTaskResult
        }

    let run country =
        fun (_, schedule, ct) ->
            Persistence.Storage.create InMemory
            |> ResultAsync.wrap (fun storage ->
                storage
                |> getRequests ct country
                |> ResultAsync.bind' (processRequests ct schedule storage))

let addTasks country =
    Graph.Node(
        { Name = "Russian"; Task = None },
        [ Graph.Node(
              { Name = "Search Appointments"
                Task = Some <| SearchAppointments.run country },
              []
          )
          Graph.Node(
              { Name = "Make Appointments"
                Task = Some <| MakeAppointments.run country },
              []
          ) ]
    )
