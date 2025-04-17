module EA.Telegram.Services.Embassies.Russian.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Telegram.Dependencies.Embassies.Russian
open EA.Telegram.Services.Embassies

let get embassyId (service: Service) =
    fun (chatId, messageId) ->
        match service.Id.Split() with
        | [ _; Embassies.RUS; _; _; "0" ] -> (chatId, messageId) |> Instruction.toCheckAppointments embassyId service
        | [ _; Embassies.RUS; _; _; "1" ] -> (chatId, messageId) |> Instruction.toAutoNotifications embassyId service
        | [ _; Embassies.RUS; _; _; "2"; "0" ] ->
            (chatId, messageId)
            |> Instruction.toAutoFirstAvailableConfirmation embassyId service
        | [ _; Embassies.RUS; _; _; "2"; "1" ] ->
            (chatId, messageId)
            |> Instruction.toAutoLastAvailableConfirmation embassyId service
        | [ _; Embassies.RUS; _; _; "2"; "2" ] ->
            (chatId, messageId) |> Instruction.toAutoDateRangeConfirmation embassyId service
        | _ ->
            $"The service '%s{service.Name}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return

let userGet embassyId (service: Service) =
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
