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
    | true -> "No services are available for you here." |> Text.create
    | false ->
        ButtonsGroup.create {
            Name = name |> Option.defaultValue "Choose the service you need"
            Columns = 1
            Buttons = buttons |> Seq.sortBy fst |> ButtonsGroup.createButtons
        }
    |> Message.tryReplace (Some messageId) chatId
    |> Ok
    |> async.Return

let private (|RUS|ITA|EmbassyNotFound|) (embassyId: EmbassyId) =
    match embassyId.Value |> Graph.NodeId.splitValues |> List.truncate 2 with
    | [ _; Embassies.RUS ] -> RUS Embassies.RUS
    | [ _; Embassies.ITA ] -> ITA Embassies.ITA
    | _ -> EmbassyNotFound

let private tryCreateServiceRootId (embassyId: EmbassyId) =
    match embassyId with
    | RUS value -> value |> Ok
    | ITA value -> value |> Ok
    | EmbassyNotFound ->
        $"Services for embassy '%s{embassyId.ValueStr}' are not implemented. "
        + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
    |> Result.map (fun embassyIdValue ->
        Graph.NodeId.combine [ Graph.NodeIdValue Services.ROOT_ID; Graph.NodeIdValue embassyIdValue ]
        |> ServiceId)

let private tryGetService (embassyId: EmbassyId) (serviceId: ServiceId) forUser =
    fun (deps: Services.Dependencies) ->
        match embassyId with
        | RUS _ ->
            deps
            |> Russian.Dependencies.create
            |> Russian.Query.getService embassyId serviceId forUser
        | ITA _ ->
            deps
            |> Italian.Dependencies.create
            |> Italian.Query.getService embassyId serviceId forUser
        | EmbassyNotFound ->
            $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return

let getService embassyId serviceId =
    fun (deps: Services.Dependencies) ->
        deps.tryFindServiceNode serviceId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) -> deps |> tryGetService embassyId serviceId false
            | Some(AP.Node node) ->
                node.Children
                |> Seq.map _.Value
                |> Seq.map (fun service ->
                    let route = Router.Services(Method.Get(Get.Service(embassyId, service.Id)))
                    service.LastName, route.Value)
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
        deps.tryFindServiceNode serviceId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) -> deps |> tryGetService embassyId serviceId true
            | Some(AP.Node node) ->

                let userServiceIds = deps.Chat.Subscriptions |> Seq.map _.ServiceId.Value

                node.Children
                |> Seq.map _.Value
                |> Seq.filter (fun service -> service.Id.Value.IsInSeq userServiceIds)
                |> Seq.map (fun service ->
                    let route = Router.Services(Method.Get(Get.UserService(embassyId, service.Id)))
                    service.LastName, route.Value)
                |> createButtonsGroup deps.Chat.Id deps.MessageId node.Value.Description
            | None ->
                $"You have no services available for '%s{serviceId.ValueStr}'."
                |> NotFound
                |> Error
                |> async.Return)

let getUserServices embassyId =
    fun (deps: Services.Dependencies) ->
        embassyId
        |> tryCreateServiceRootId
        |> ResultAsync.wrap (fun serviceId -> deps |> getUserService embassyId serviceId)
