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
                     PageSize = 20
                     SortBy = Filter.Asc(Filter.Date(_.Modified)) }
              Ids = None
              Embassies = Some <| Set [ Russian country ]
              HasStates = Some <| fun state -> state <> InProcess
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
            let! results = requests |> Seq.map processRequest |> Async.Sequential

            return
                results
                |> Seq.map (function
                    | Ok request ->
                        match request |> handleProcessedRequest with
                        | Ok msg -> msg
                        | Error error -> error.Message
                    | Error error -> error.Message)
                |> Seq.fold (fun acc msg -> $"{acc}\n{msg}") ""
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

let testRun country =
    fun (_, schedule, ct) ->
        async {

            do! Async.Sleep 2000
            return Ok <| Info $"Test run for {country}"

        }

let addTasks country =
    Graph.Node(
        { Name = "Russian"; Task = None },
        [ Graph.Node(
              { Name = "Search Appointments"
                Task = Some <| testRun country },
              []
          ) ]
    )
