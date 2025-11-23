module internal EA.Worker.Embassies.Russian

open Infrastructure.Prelude
open Infrastructure.Logging
open EA.Core.Domain
open EA.Worker.Dependencies.Embassies.Russian

module Kdmid =
    open EA.Russian.Services.Domain.Kdmid

    let private processGroup requests =
        fun (deps: Kdmid.Dependencies) ->
            deps.tryProcessFirst requests
            |> ResultAsync.map (fun request ->
                match request.Payload.State with
                | NoAppointments -> deps.TaskName + " No appointments found." |> Log.dbg
                | HasAppointments appointments ->
                    deps.TaskName + $" Appointments found: %i{appointments.Count}" |> Log.scs
                | HasConfirmation(msg, appointment) ->
                    deps.TaskName
                    + $" Confirmation found: %s{msg}. %s{Appointment.print appointment}"
                    |> Log.scs)

    let private start =
        fun (deps: Kdmid.Dependencies) ->
            let inline processGroup requests = deps |> processGroup requests

            [ Services.ROOT_ID; Embassies.RUS ]
            |> ServiceId.combine
            |> deps.getRequests
            |> ResultAsync.map (fun requests ->
                requests
                |> Seq.groupBy _.Service.Id
                |> Seq.map (fun (_, requests) ->
                    requests
                    |> Seq.sortByDescending _.Modified
                    |> Seq.truncate 5
                    |> Seq.toList
                    |> processGroup))
            |> ResultAsync.map (Async.Sequential >> Async.map Result.unzip)
            |> Async.bind (function
                | Error error -> Error error |> async.Return
                | Ok results ->
                    results
                    |> Async.map (fun (_, errors) ->
                        errors |> Seq.iter (fun error -> deps.TaskName + error.Message |> Log.crt) |> Ok))
            |> ResultAsync.apply (deps.cleanResources ())

    module SearchAppointments =
        let handle (task, di, ct) =
            Kdmid.Dependencies.create task di ct |> ResultAsync.wrap start
