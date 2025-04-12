module EA.Telegram.Services.Embassies.Russian.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Telegram.Dependencies.Embassies.Russian
open EA.Telegram.Services.Embassies.Russian

let get embassyId (service: ServiceNode) =
    fun (deps: Russian.Dependencies) ->
        match service.Id.Split() with
        | [ _; Embassies.RUS; _; _; "0" ] ->
            deps
            |> Kdmid.Dependencies.create
            |> ResultAsync.wrap (Kdmid.Message.Instruction.toCheckAppointments embassyId service)
        | [ _; Embassies.RUS; _; _; "1" ] ->
            deps
            |> Kdmid.Dependencies.create
            |> ResultAsync.wrap (Kdmid.Message.Instruction.toAutoNotifications embassyId service)
        | [ _; Embassies.RUS; _; _; "2"; "0" ] ->
            deps
            |> Kdmid.Dependencies.create
            |> ResultAsync.wrap (Kdmid.Message.Instruction.toAutoFirstAvailableConfirmation embassyId service)
        | [ _; Embassies.RUS; _; _; "2"; "1" ] ->
            deps
            |> Kdmid.Dependencies.create
            |> ResultAsync.wrap (Kdmid.Message.Instruction.toAutoLastAvailableConfirmation embassyId service)
        | [ _; Embassies.RUS; _; _; "2"; "2" ] ->
            deps
            |> Kdmid.Dependencies.create
            |> ResultAsync.wrap (Kdmid.Message.Instruction.toAutoDateRangeConfirmation embassyId service)
        | _ ->
            $"The service '%s{service.Name}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
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
            | [ _; Embassies.RUS; _; _; "0" ]
            | [ _; Embassies.RUS; _; _; "1" ]
            | [ _; Embassies.RUS; _; _; "2"; "0" ]
            | [ _; Embassies.RUS; _; _; "2"; "1" ]
            | [ _; Embassies.RUS; _; _; "2"; "2" ] ->
                deps
                |> Kdmid.Dependencies.create
                |> Result.bind (Kdmid.Query.getSubscriptions requests)
            | _ ->
                $"The service '%s{service.Name}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error)
