module EA.Telegram.Services.Services.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Services
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Services
open EA.Telegram.Dependencies.Services.Russian
open EA.Telegram.Dependencies.Services.Italian

let private createButtonsGroup chatId messageId name buttons =
    ButtonsGroup.create {
        Name = name |> Option.defaultValue "Choose what you need to get"
        Columns = 3
        Buttons =
            buttons
            |> Seq.map (fun (callback, name) -> Button.create name (CallbackData callback))
            |> Set.ofSeq
    }
    |> Message.tryReplace (Some messageId) chatId
    |> Ok
    |> async.Return

let private tryDefineService (embassyId: EmbassyId) (serviceId: ServiceId) =
    fun (deps: Services.Dependencies) ->
        match embassyId.Value.Split() |> Seq.skip 1 |> Seq.tryHead with
        | Some embassy ->
            match embassy with
            | Embassies.RUS ->
                Russian.Dependencies.create deps
                |> ResultAsync.wrap (Russian.Query.getService embassyId serviceId)
            | Embassies.ITA ->
                Italian.Dependencies.create deps
                |> ResultAsync.wrap (Italian.Query.getService embassyId serviceId)
            | _ ->
                $"Service for '%s{embassy}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
                |> async.Return
        | None ->
            $"Service for '%s{embassyId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return

let getService embassyId serviceId =
    fun (deps: Services.Dependencies) ->
        deps.getServiceNode serviceId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) -> deps |> tryDefineService embassyId serviceId
            | Some(AP.Node node) ->
                node.Children
                |> Seq.map _.Value
                |> Seq.map (fun service ->
                    let route = Router.Services(Method.Get(Get.Service(embassyId, service.Id)))
                    route.Value, service.Name)
                |> createButtonsGroup deps.Chat.Id deps.MessageId node.Value.Description
            | None ->
                $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
                |> async.Return)

let getServices embassyId =
    fun (deps: Services.Dependencies) ->
        let serviceId = Services.ROOT_ID |> Graph.NodeIdValue |> ServiceId
        deps |> getService embassyId serviceId

let getUserService embassyId serviceId =
    fun (deps: Services.Dependencies) ->
        deps.getServiceNode serviceId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) -> deps |> tryDefineService embassyId serviceId
            | Some(AP.Node node) ->

                let userServiceIds = deps.Chat.Subscriptions |> Seq.map _.ServiceId.Value

                node.Children
                |> Seq.map _.Value
                |> Seq.filter (fun service -> service.Id.Value.In userServiceIds)
                |> Seq.map (fun service ->
                    let route = Router.Services(Method.Get(Get.UserService(embassyId, service.Id)))
                    route.Value, service.Name)
                |> createButtonsGroup deps.Chat.Id deps.MessageId node.Value.Description
            | None ->
                $"You have no services for '%s{embassyId.ValueStr}' in your list."
                |> NotFound
                |> Error
                |> async.Return)

let getUserServices embassyId =
    fun (deps: Services.Dependencies) ->
        let serviceId = Services.ROOT_ID |> Graph.NodeIdValue |> ServiceId
        deps |> getService embassyId serviceId
