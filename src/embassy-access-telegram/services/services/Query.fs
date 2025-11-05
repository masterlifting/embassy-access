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
    match embassyId.NodeId.Values[1] with
    | Embassies.RUS -> RUS Embassies.RUS
    | Embassies.ITA -> ITA Embassies.ITA
    | _ -> EmbassyNotFound

let private tryCreateServiceRootId (embassyId: EmbassyId) =
    match embassyId with
    | RUS value -> value |> Ok
    | ITA value -> value |> Ok
    | EmbassyNotFound ->
        $"Services for embassy '%s{embassyId.Value}' are not implemented. "
        + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
    |> Result.map (fun embassyIdValue -> ServiceId.combine [ Services.ROOT_ID; embassyIdValue ])

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
            $"Service '%s{serviceId.Value}' is not implemented. " + NOT_IMPLEMENTED
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
                |> Seq.map (fun service ->
                    let serviceId = service.Id |> ServiceId
                    let route = Router.Services(Method.Get(Get.Service(embassyId, serviceId)))
                    service.Value.LastName, route.Value)
                |> createButtonsGroup deps.Chat.Id deps.MessageId node.Value.Description
            | None ->
                $"Service '%s{serviceId.Value}' is not implemented. " + NOT_IMPLEMENTED
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
                |> Seq.filter (fun service -> service.Id |> Tree.NodeId.contains userServiceIds)
                |> Seq.map (fun service ->
                    let serviceId = service.Id |> ServiceId
                    let route = Router.Services(Method.Get(Get.UserService(embassyId, serviceId)))
                    service.Value.LastName, route.Value)
                |> createButtonsGroup deps.Chat.Id deps.MessageId node.Value.Description
            | None ->
                $"You have no services available for '%s{serviceId.Value}'."
                |> NotFound
                |> Error
                |> async.Return)

let getUserServices embassyId =
    fun (deps: Services.Dependencies) ->
        embassyId
        |> tryCreateServiceRootId
        |> ResultAsync.wrap (fun serviceId -> deps |> getUserService embassyId serviceId)
