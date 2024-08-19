module internal EmbassyAccess.Worker.Embassies.Russian

open Infrastructure
open Persistence.Domain
open Worker.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Persistence
open EmbassyAccess.Embassies.Russian.Domain

module private SearchAppointments =

    let private getRequests ct country storage =
        let filter: Filter.Request =
            { Pagination =
                Some
                <| { Page = 1
                     PageSize = 5
                     SortBy = Filter.Desc(Filter.Date(_.Modified)) }
              Embassies = Some <| Set [ Russian country ]
              HasStates = Some <| fun state -> state <> InProcess
              Ids = None
              HasAppointments = None
              HasConfirmations = None
              HasConfirmationState = None
              WasModified = None }

        storage |> Repository.Query.Request.get ct filter

    let private handleProcessedRequest request =
        match request.State with
        | Failed error -> Error error
        | Completed msg ->
            match request.Appointments.IsEmpty with
            | true -> Ok $"No appointments found. {request.Payload}"
            | false ->
                match request.Appointments |> Seq.choose (fun x -> x.Confirmation) |> List.ofSeq with
                | [] -> Ok $"Found appointments. {msg}"
                | _ -> Ok $"Found confirmations. {msg}"
        | state -> Ok $"Request {request.Id.Value} state is in complete. Current state: {state}"

    let private handleProcessedErrors errors =
        let errorMessage = errors |> Seq.fold (fun acc x -> $"{acc}\n\t{x}") ""
        Operation { Message = errorMessage; Code = None }

    let private handleProcessedRequests requests =
        requests
        |> Seq.map handleProcessedRequest
        |> Seq.roes
        |> Result.mapError handleProcessedErrors
        |> Result.map (Seq.fold (fun acc x -> $"{acc}\n\t{x}") "")
        |> Result.map Info

    let private processRequests ct (schedule: Schedule option) storage requests =
        let config =
            { TimeShift =
                match schedule with
                | None -> 0y
                | Some scheduler -> scheduler.TimeShift }

        let processRequest =
            (storage, config, ct)
            |> EmbassyAccess.Deps.Russian.processRequest
            |> EmbassyAccess.Api.processRequest

        async {
            let! results = requests |> Seq.map processRequest |> Async.Parallel

            return
                results
                |> Seq.map (fun result ->
                    match result with
                    | Ok request ->
                        match request |> handleProcessedRequest with
                        | Ok msg -> msg
                        | Error error -> error.Message
                    | Error error -> error.Message)
                |> Seq.fold (fun acc x -> $"{acc}\n\t{x}") ""
                |> Info
                |> Ok
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
          ) ]
    )
