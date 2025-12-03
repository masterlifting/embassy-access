module internal EA.Worker.Embassies.Italian

open Infrastructure.Prelude
open Infrastructure.Logging
open EA.Core.Domain
open EA.Worker.Dependencies.Embassies.Italian

module Prenotami =
    open EA.Italian.Services.Domain.Prenotami

    let private processGroup requests =
        fun (deps: Prenotami.Dependencies) ->
            deps.tryProcessFirst requests
            |> ResultAsync.map (fun request ->
                match request.Payload.State with
                | NoAppointments msg -> deps.TaskName + $" {msg}." |> Log.dbg
                | HasAppointments appointments ->
                    deps.TaskName + $" Appointments found: %i{appointments.Count}" |> Log.scs)

    let private start =
        fun (deps: Prenotami.Dependencies) ->
            let inline processGroup requests = deps |> processGroup requests

            [ Services.ROOT_ID; Embassies.ITA ]
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
            |> ResultAsync.apply deps.cleanResources

    module SearchAppointments =
        let handle (task, deps, ct) =
            Prenotami.Dependencies.create task deps ct |> ResultAsync.wrap start
