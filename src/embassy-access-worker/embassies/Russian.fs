module internal EmbassyAccess.Worker.Embassies.Russian

open Infrastructure
open Persistence.Domain
open Worker.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Persistence

module private SearchAppointments =

    let private getRequests ct country storage =
        let filter: Filter.Request =
            { Pagination =
                Some
                <| { Page = 1
                     PageSize = 5
                     SortBy = Filter.Desc(Filter.Date(_.Modified)) }
              Embassies = Some <| Set [ Russian country ]
              HasStates = Some(fun state -> state <> InProcess)
              Ids = None
              HasAppointments = false
              HasConfirmations = false
              WasModified = None }

        storage |> Repository.Query.Request.get ct filter

    let private tryGetAppointments ct scheduler storage requests =

        let config: EmbassyAccess.Embassies.Russian.Domain.Configuration =
            { TimeShift = 
                match scheduler with
                | None -> 0y
                | Some scheduler -> scheduler.TimeShift
             }

        let getAppointments =
            (storage, config, ct)
            |> EmbassyAccess.Deps.Russian.getAppointments
            |> EmbassyAccess.Api.getAppointments

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
                    match! request |> getAppointments with
                    | Error error -> return! innerLoop requestsTail (errors @ [ error ])
                    | Ok result ->
                        match result.State with
                        | Failed error -> return! innerLoop requestsTail (errors @ [ error ])
                        | _ -> return Ok <| Some result
            }

        innerLoop requests []

    let private handleAppointmentsResponse request =
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
        fun (_, scheduler, ct) ->
            Persistence.Storage.create InMemory
            |> ResultAsync.wrap (fun storage ->
                storage
                |> getRequests ct country
                |> ResultAsync.bind' (tryGetAppointments ct scheduler storage)
                |> ResultAsync.bind handleAppointmentsResponse)

let createNode country =
    Graph.Node(
        { Name = "Russian"; Handle = None },
        [ Graph.Node(
              { Name = "Search Appointments"
                Handle = Some <| SearchAppointments.run country },
              []
          ) ]
    )
