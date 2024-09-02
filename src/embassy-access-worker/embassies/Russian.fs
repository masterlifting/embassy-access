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
                     SortBy = Filter.Date _.Modified |> Filter.Asc }
              Ids = None
              Embassies = Some <| Set [ Russian country ]
              HasStates =
                Some
                <| function
                    | InProcess -> false
                    | _ -> true
              HasAppointments = None
              HasConfirmations = None
              HasConfirmationState =
                Some
                <| function
                    | Auto _ -> false
                    | _ -> true
              WasModified = None }

        storage |> Repository.Query.Request.get ct filter

    let private handleProcessedRequest request =
        match request.State with
        | Failed error -> Error error
        | Completed msg -> Ok msg
        | state -> Ok $"Request {request.Id.Value} is not completed. Current state: {state}"

    let private processRequests ct (schedule: Schedule option) storage requests =
        let config =
            { TimeShift =
                schedule
                |> Option.map (fun schedule -> schedule.TimeShift)
                |> Option.defaultValue 0y }

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
                |> Seq.foldi (fun state index msg -> $"%s{state}{System.Environment.NewLine}%i{index + 1}. %s{msg}") ""
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

module private MakeAppointments =

    let private getRequests ct country storage =
        let filter: Filter.Request =
            { Pagination =
                Some
                <| { Page = 1
                     PageSize = 20
                     SortBy = Filter.Date _.Modified |> Filter.Asc }
              Ids = None
              Embassies = Some <| Set [ Russian country ]
              HasStates =
                Some
                <| function
                    | InProcess -> false
                    | _ -> true
              HasAppointments = None
              HasConfirmations = None
              HasConfirmationState =
                Some
                <| function
                    | Auto _ -> true
                    | _ -> false
              WasModified = None }

        storage |> Repository.Query.Request.get ct filter

    let run country =
        fun (_, schedule, ct) -> async { return Ok <| Success $"{country}" }

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
