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

let private (|RUS|ITA|EmbassyNotFound|) (embassyId: EmbassyId) =
    match embassyId.Value.Split() |> Seq.skip 1 |> Seq.tryHead with
    | Some id ->
        match id with
        | Embassies.RUS -> RUS
        | Embassies.ITA -> ITA
        | _ -> EmbassyNotFound
    | _ -> EmbassyNotFound

let private tryCreateServiceRootId (embassyId: EmbassyId) =
    match embassyId with
    | RUS -> Embassies.RUS |> Ok
    | ITA -> Embassies.ITA |> Ok
    | EmbassyNotFound ->
        $"Embassy '%s{embassyId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
    |> Result.map (fun embassyIdValue ->
        Graph.NodeId.combine [ Graph.NodeIdValue Services.ROOT_ID; Graph.NodeIdValue embassyIdValue ]
        |> ServiceId)

let private tryGetService (embassyId: EmbassyId) (serviceId: ServiceId) =
    fun (deps: Services.Dependencies) ->
        match embassyId with
        | RUS ->
            Russian.Dependencies.create deps
            |> ResultAsync.wrap (Russian.Query.getService embassyId serviceId)
        | ITA ->
            Italian.Dependencies.create deps
            |> ResultAsync.wrap (Italian.Query.getService embassyId serviceId)
        | EmbassyNotFound ->
            $"Service for '%s{embassyId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return

let getService embassyId serviceId =
    fun (deps: Services.Dependencies) ->
        deps.getServiceNode serviceId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) -> deps |> tryGetService embassyId serviceId
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
        embassyId
        |> tryCreateServiceRootId
        |> ResultAsync.wrap (fun serviceId -> deps |> getService embassyId serviceId)

let getUserService embassyId serviceId =
    fun (deps: Services.Dependencies) ->
        deps.getServiceNode serviceId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) -> deps |> tryGetService embassyId serviceId
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
        embassyId
        |> tryCreateServiceRootId
        |> ResultAsync.wrap (fun serviceId -> deps |> getUserService embassyId serviceId)
