module internal EmbassyAccess.Worker.Embassies.Russian

open System
open Infrastructure
open Persistence.Domain
open Worker.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Persistence
open EmbassyAccess.Embassies.Russian.Domain

let private createConfig (schedule: Schedule option) =
    { TimeShift = schedule |> Option.map _.TimeShift |> Option.defaultValue 0y }

let private notifySubscribers request = async { return Ok request }

let private processRequest ct config storage request =

    let processRequest ct config storage =
        (storage, config, ct)
        |> EmbassyAccess.Deps.Russian.processRequest
        |> EmbassyAccess.Api.processRequest

    processRequest ct config storage request
    |> ResultAsync.bind' notifySubscribers
    |> ResultAsync.map (fun request -> $"Result: {request.State}.")

let private toTaskResult (results: Result<string, Error'> array) =
    results
    |> Seq.raes
    |> Seq.map (function
        | Some msg, None -> msg
        | None, Some error -> error.Message
        | Some msg, Some error -> $"%s{msg}; %s{error.Message}"
        | None, None -> "No results")
    |> String.concat Environment.NewLine
    |> Info
    |> Ok

let private run country getRequests processRequests =
    fun (_, schedule, ct) ->
        Persistence.Storage.create InMemory
        |> ResultAsync.wrap (fun storage ->
            storage
            |> getRequests ct country
            |> ResultAsync.bind' (processRequests ct schedule storage))

module private SearchAppointments =

    let private getRequests ct country storage =
        let filter = Russian country |> Filter.Request.SearchAppointments
        storage |> Repository.Query.Request.get ct filter

    let private processRequests ct schedule storage requests =
        let config = createConfig schedule
        let processRequest = processRequest ct config storage

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
                    match! group |> Seq.toList |> choose [] with
                    | Ok result ->
                        match result with
                        | Some result -> return Ok $"Group '%s{name}': %s{result}"
                        | None -> return Ok $"Group '%s{name}'. No results."
                    | Error error -> return Error error
                }

            groups |> Map.map processGroup |> Map.values |> Async.Parallel

        async {
            let! results = groupedRequests |> processGroupedRequests
            return results |> toTaskResult
        }

    let run country = run country getRequests processRequests

module private MakeAppointments =

    let private getRequests ct country storage =
        let filter = Russian country |> Filter.Request.MakeAppointments
        storage |> Repository.Query.Request.get ct filter

    let private processRequests ct schedule storage requests =
        let config = createConfig schedule
        let processRequest = processRequest ct config storage

        async {
            let! results = requests |> Seq.map processRequest |> Async.Sequential
            return results |> toTaskResult
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
