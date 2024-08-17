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

    let private tryProcessRequests ct (schedule: Schedule option) storage requests =

        let config =
            { TimeShift =
                match schedule with
                | None -> 0y
                | Some scheduler -> scheduler.TimeShift }

        let processRequest =
            (storage, config, ct)
            |> EmbassyAccess.Deps.Russian.processRequest
            |> EmbassyAccess.Api.processRequest

        let rec innerLoop requests (errors: Error' list) =
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
                                |> List.mapi (fun i error -> $"{i + 1}.{error.Message}")
                                |> String.concat "\n"

                            Error
                            <| Operation
                                { Message = $"Multiple errors: \n{msg}"
                                  Code = None }

                | request :: requestsTail ->
                    match! request |> processRequest with
                    | Error error -> return! innerLoop requestsTail (errors @ [ error ])
                    | Ok result ->
                        match result.State with
                        | Failed error -> return! innerLoop requestsTail (errors @ [ error ])
                        | _ -> return Ok <| Some result
            }

        innerLoop requests []

    let private handleProcessedRequest request =
        match request with
        | None -> Ok <| Info "No appointments found."
        | Some request ->
            match request.State with
            | Failed error -> Error error
            | _ ->
                Ok
                <| match request.Appointments.IsEmpty with
                   | true -> Info "No appointments found."
                   | false -> Success $"Found {request.Appointments.Count} appointments."

    let run country =
        fun (_, schedule, ct) ->
            Persistence.Storage.create InMemory
            |> ResultAsync.wrap (fun storage ->
                storage
                |> getRequests ct country
                |> ResultAsync.bind' (tryProcessRequests ct schedule storage)
                |> ResultAsync.bind handleProcessedRequest)

let addTasks country =
    Graph.Node(
        { Name = "Russian"; Task = None },
        [ Graph.Node(
              { Name = "Search Appointments"
                Task = Some <| SearchAppointments.run country },
              []
          ) ]
    )
