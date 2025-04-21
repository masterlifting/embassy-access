module EA.Telegram.Services.Services.Russian.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Telegram.Dependencies.Services.Russian
open EA.Telegram.Services.Embassies
open EA.Telegram.Router
open EA.Telegram.Router.Services

let private createButtonsGroup chatId messageId name buttons =
    ButtonsGroup.create {
        Name = name |> Option.defaultValue "Choose what you need to visit"
        Columns = 3
        Buttons =
            buttons
            |> Seq.map (fun (callback, name) -> Button.create name (CallbackData callback))
            |> Set.ofSeq
    }
    |> Message.tryReplace (Some messageId) chatId

let private (|Kdmid|Midpass|ServiceNotFound|) (serviceId: ServiceId) =
    match serviceId.Value.Split() with
    | [ _; _; _; _; _; "0" ]
    | [ _; _; _; _; _; "1" ]
    | [ _; _; _; _; _; "2" ]
    | [ _; _; _; _; _; "2"; "0" ]
    | [ _; _; _; _; _; "2"; "1" ]
    | [ _; _; _; _; _; "2"; "2" ] -> Kdmid
    | [ _; _; "0"; "0"; "1" ] -> Midpass
    | _ -> ServiceNotFound

let getService serviceId (embassyId: EmbassyId) =
    fun (deps: Russian.Dependencies) ->
        deps.getServiceNode serviceId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) ->
                match serviceId with
                | Kdmid ->
                    Kdmid.Dependencies.create deps
                    |> ResultAsync.wrap (Kdmid.Query.getService serviceId embassyId)
                | Midpass ->               
                    Midpass.Dependencies.create deps
                    |> ResultAsync.wrap (Midpass.Query.getService serviceId embassyId)
                | ServiceNotFound -> 
                    $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
                    |> NotImplemented
                    |> Error
                    |> async.Return
            | Some(AP.Node node) ->
                node.Children
                |> Seq.map _.Value
                |> Seq.map (fun service ->
                    let route = Router.Services(Method.Russian(Russian.Method.Get(Get.Service(embassyId, service.Id))))
                    route.Value, service.Name)
                |> createButtonsGroup deps.Chat.Id deps.MessageId node.Value.Description
                |> Ok
                |> async.Return
            | None ->
                $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
                |> async.Return)

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
        
let getUserService (serviceId: ServiceId) (embassyId: EmbassyId) =
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

let getUserServices (embassyId: EmbassyId) =
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
