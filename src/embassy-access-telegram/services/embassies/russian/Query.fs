﻿module EA.Telegram.Services.Embassies.Russian.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Telegram.Domain
open EA.Telegram.Dependencies.Embassies.Russian
open EA.Telegram.Services.Embassies.Russian

let get embassyId (service: ServiceNode) =
    fun (deps: Russian.Dependencies) ->
        match service.Id.Split() with
        | [ _; Constants.RUSSIAN_NODE_ID; _; _; "0" ] ->
            deps
            |> Kdmid.Dependencies.create
            |> ResultAsync.wrap (Kdmid.Message.Instruction.toCheckAppointments embassyId service)
        | [ _; Constants.RUSSIAN_NODE_ID; _; _; "1" ] ->
            deps
            |> Kdmid.Dependencies.create
            |> ResultAsync.wrap (Kdmid.Message.Instruction.toStandardSubscribe embassyId service)
        | [ _; Constants.RUSSIAN_NODE_ID; _; _; "2"; "0" ] ->
            deps
            |> Kdmid.Dependencies.create
            |> ResultAsync.wrap (Kdmid.Message.Instruction.toFirstAvailableAutoSubscribe embassyId service)
        | [ _; Constants.RUSSIAN_NODE_ID; _; _; "2"; "1" ] ->
            deps
            |> Kdmid.Dependencies.create
            |> ResultAsync.wrap (Kdmid.Message.Instruction.toLastAvailableAutoSubscribe embassyId service)
        | [ _; Constants.RUSSIAN_NODE_ID; _; _; "2"; "2" ] ->
            deps
            |> Kdmid.Dependencies.create
            |> ResultAsync.wrap (Kdmid.Message.Instruction.toDateRangeAutoSubscribe embassyId service)
        | _ ->
            $"The '%s{service.ShortName}' is not supported."
            |> NotSupported
            |> Error
            |> async.Return

let userGet embassyId (service: ServiceNode) =
    fun (deps: Russian.Dependencies) ->
        deps.getChatRequests ()
        |> ResultAsync.map (
            List.filter (fun request -> request.Service.Id = service.Id && request.Service.Embassy.Id = embassyId)
        )
        |> ResultAsync.bind (fun requests ->
            match service.Id.Split() with
            | [ _; Constants.RUSSIAN_NODE_ID; _; _; "0" ]
            | [ _; Constants.RUSSIAN_NODE_ID; _; _; "1" ]
            | [ _; Constants.RUSSIAN_NODE_ID; _; _; "2"; "0" ]
            | [ _; Constants.RUSSIAN_NODE_ID; _; _; "2"; "1" ]
            | [ _; Constants.RUSSIAN_NODE_ID; _; _; "2"; "2" ] ->
                deps
                |> Kdmid.Dependencies.create
                |> Result.bind (Kdmid.Query.getSubscriptions requests)
            | _ -> $"The '%s{service.ShortName}' is not supported." |> NotSupported |> Error)
