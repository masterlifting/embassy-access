module EA.Telegram.Services.Embassies.Italian.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Telegram.Dependencies.Embassies.Italian
open EA.Telegram.Services.Embassies

let get embassyId (service: Service) =
    fun (chatId, messageId) ->
        match service.Id.Split() with
        | [ _; Embassies.ITA; _; _; "0" ] -> (chatId, messageId) |> Instruction.toCheckAppointments embassyId service
        | [ _; Embassies.ITA; _; _; "1" ] -> (chatId, messageId) |> Instruction.toAutoNotifications embassyId service
        | [ _; Embassies.ITA; _; _; "2"; "0" ] ->
            (chatId, messageId)
            |> Instruction.toAutoFirstAvailableConfirmation embassyId service
        | [ _; Embassies.ITA; _; _; "2"; "1" ] ->
            (chatId, messageId)
            |> Instruction.toAutoLastAvailableConfirmation embassyId service
        | [ _; Embassies.ITA; _; _; "2"; "2" ] ->
            (chatId, messageId) |> Instruction.toAutoDateRangeConfirmation embassyId service
        | _ ->
            $"The service '%s{service.Name}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return

let userGet embassyId (service: Service) =
    fun (deps: Italian.Dependencies) ->
        deps.getChatRequests ()
        |> ResultAsync.map (
            List.filter (fun request -> request.Service.Id = service.Id && request.Service.Embassy.Id = embassyId)
        )
        |> ResultAsync.bind (fun requests ->
            match service.Id.Split() with
            | [ _; Embassies.ITA; _; _; "0" ]
            | [ _; Embassies.ITA; _; _; "1" ]
            | [ _; Embassies.ITA; _; _; "2"; "0" ]
            | [ _; Embassies.ITA; _; _; "2"; "1" ]
            | [ _; Embassies.ITA; _; _; "2"; "2" ] ->
                deps
                |> Prenotami.Dependencies.create
                |> Prenotami.Query.getSubscriptions requests
            | _ ->
                $"The service '%s{service.Name}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error)
