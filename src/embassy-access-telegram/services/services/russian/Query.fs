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
    |> Ok
    |> async.Return

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

let getService embassyId serviceId =
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
                    let route =
                        Router.Services(Method.Russian(Russian.Method.Get(Get.Service(embassyId, service.Id))))
                    route.Value, service.Name)
                |> createButtonsGroup deps.Chat.Id deps.MessageId node.Value.Description
            | None ->
                $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
                |> async.Return)

let getServices embassyId =
    fun (deps: Russian.Dependencies) ->
        let serviceId =
            Graph.NodeId.combine [ Services.ROOT_ID |> Graph.NodeIdValue; Embassies.RUS |> Graph.NodeIdValue ]
            |> ServiceId
        deps |> getService embassyId serviceId

let getUserService serviceId embassyId =
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

                let userServiceIds = deps.Chat.Subscriptions |> Seq.map _.ServiceId.Value

                node.Children
                |> Seq.map _.Value
                |> Seq.filter (fun service -> service.Id.Value.In userServiceIds)
                |> Seq.map (fun service ->
                    let route =
                        Router.Services(Method.Russian(Russian.Method.Get(Get.UserService(embassyId, service.Id))))
                    route.Value, service.Name)
                |> createButtonsGroup deps.Chat.Id deps.MessageId node.Value.Description
            | None ->
                $"You have no services for '%s{embassyId.ValueStr}' in your list."
                |> NotFound
                |> Error
                |> async.Return)

let getUserServices embassyId =
    fun (deps: Russian.Dependencies) ->
        let serviceId =
            Graph.NodeId.combine [ Services.ROOT_ID |> Graph.NodeIdValue; Embassies.RUS |> Graph.NodeIdValue ]
            |> ServiceId
        deps |> getService embassyId serviceId
