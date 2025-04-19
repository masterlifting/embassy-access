module EA.Telegram.Services.Services.Russian.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Telegram.Dependencies.Services.Russian
open EA.Telegram.Services.Embassies

let getService (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Russian.Dependencies) ->
        deps.getServiceGraph embassyId
        |> ResultAsync.bind (function
            | AP.Leaf value ->
                match
                    value.Id.Value
                    |> Graph.NodeId.split
                    |> Seq.skip 1
                    |> Seq.tryHead
                    |> Option.map _.Value
                with
                | Some countryId ->
                    match countryId with
                    | Embassies.RUS ->
                        let route = Router.Services(Services.Method.Russian(Services.Russian.Method.Get(Services.Get.Services value.Id)))
                    | Embassies.ITA ->
                        let route = Router.Services(Services.Method.Italian(Services.Italian.Method.Get(Services.Get.Services value.Id)))
                    | _ ->
                        $"Embassy '%s{value.Name}' is not implemented. " + NOT_IMPLEMENTED
                        |> NotImplemented
                        |> Error
                | None ->
                    $"Embassy '%s{value.Name}' is not implemented. " + NOT_IMPLEMENTED
                    |> NotImplemented
                    |> Error
            | AP.Node node ->
                node.Children
                |> Seq.map _.Value
                |> toButtonsGroup deps.Chat.Id deps.MessageId node.Value.Description
                |> Ok)

let getServices (embassyId: EmbassyId) =
    fun (deps: Russian.Dependencies) ->
        let serviceId =
            Graph.NodeId.combine
                [
                    Services.ROOT_ID |> Graph.NodeIdValue
                    Embassies.RUS |> Graph.NodeIdValue
                ]
                |> ServiceId
        deps |> getService serviceId embassyId
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
