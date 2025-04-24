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
    match buttons |> Seq.isEmpty with
    | true -> "No available services for you here." |> Text.create
    | false ->
        ButtonsGroup.create {
            Name = name |> Option.defaultValue "Choose the service you want to get"
            Columns = 1
            Buttons =
                buttons
                |> Seq.map (fun (callback, name) -> Button.create name (CallbackData callback))
                |> Set.ofSeq
        }
    |> Message.tryReplace (Some messageId) chatId
    |> Ok
    |> async.Return

let private (|RUS|ITA|EmbassyNotFound|) (embassyId: EmbassyId) =
    match embassyId.Value |> Graph.NodeId.splitValues |> Seq.skip 1 |> Seq.tryHead with
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
        $"Services for embassy '%s{embassyId.ValueStr}' is not implemented. "
        + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
    |> Result.map (fun embassyIdValue ->
        Graph.NodeId.combine [ Graph.NodeIdValue Services.ROOT_ID; Graph.NodeIdValue embassyIdValue ]
        |> ServiceId)

let private tryGetService (embassyId: EmbassyId) (serviceId: ServiceId) =
    fun (deps: Services.Dependencies) ->
        match embassyId with
        | RUS ->
            deps
            |> Russian.Dependencies.create
            |> Russian.Query.getService embassyId serviceId
        | ITA ->
            deps
            |> Italian.Dependencies.create
            |> Italian.Query.getService embassyId serviceId
        | EmbassyNotFound ->
            $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
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
                |> Seq.filter (fun service -> service.Id.Value.IsInSeq userServiceIds)
                |> Seq.map (fun service ->
                    let route = Router.Services(Method.Get(Get.UserService(embassyId, service.Id)))
                    route.Value, service.Name)
                |> createButtonsGroup deps.Chat.Id deps.MessageId node.Value.Description
            | None ->
                $"You have no services for '%s{serviceId.ValueStr}'."
                |> NotFound
                |> Error
                |> async.Return)

let getUserServices embassyId =
    fun (deps: Services.Dependencies) ->
        embassyId
        |> tryCreateServiceRootId
        |> ResultAsync.wrap (fun serviceId -> deps |> getUserService embassyId serviceId)
