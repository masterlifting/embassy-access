module EA.Telegram.Features.Embassies.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router.Embassies
open EA.Telegram.Features.Dependencies.Embassies

let private buildRoute route =
    EA.Telegram.Router.Route.Embassies route

let private createServiceButtonsGroup chatId messageId name buttons =
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

let private createEmbassyButtonsGroup chatId messageId name buttons =
    match buttons |> Seq.isEmpty with
    | true -> "No embassies are available for you here." |> Text.create
    | false ->
        ButtonsGroup.create {
            Name = name |> Option.defaultValue "Choose the embassy you want to visit"
            Columns = 3
            Buttons = buttons |> Seq.sortBy fst |> ButtonsGroup.createButtons
        }
    |> Message.tryReplace messageId chatId
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
    fun (deps: Root.Dependencies) ->
        match embassyId with
        | RUS _ ->
            deps
            |> Russian.Root.Dependencies.create
            |> Russian.Query.getService embassyId serviceId forUser
        | ITA _ ->
            deps
            |> Italian.Root.Dependencies.create
            |> Italian.Query.getService embassyId serviceId forUser
        | EmbassyNotFound ->
            $"Service '%s{serviceId.Value}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return

// Service API
let getService embassyId serviceId =
    fun (deps: Root.Dependencies) ->
        deps.tryFindServiceNode serviceId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) -> deps |> tryGetService embassyId serviceId false
            | Some(AP.Node node) ->
                node.Children
                |> Seq.map (fun service ->
                    let serviceId = service.Id |> ServiceId
                    let route = Get(Service(embassyId, serviceId)) |> buildRoute
                    service.Value.LastName, route.Value)
                |> createServiceButtonsGroup deps.Chat.Id deps.MessageId node.Value.Description
            | None ->
                $"Service '%s{serviceId.Value}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
                |> async.Return)

let getServices embassyId =
    fun (deps: Root.Dependencies) ->
        embassyId
        |> tryCreateServiceRootId
        |> ResultAsync.wrap (fun serviceId -> deps |> getService embassyId serviceId)

let getUserService embassyId serviceId =
    fun (deps: Root.Dependencies) ->
        deps.tryFindServiceNode serviceId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) -> deps |> tryGetService embassyId serviceId true
            | Some(AP.Node node) ->

                let userServiceIds = deps.Chat.Subscriptions |> Seq.map _.ServiceId.NodeId

                node.Children
                |> Seq.filter (fun service -> userServiceIds |> Tree.NodeId.contains service.Id)
                |> Seq.map (fun service ->
                    let serviceId = service.Id |> ServiceId
                    let route = Get(UserService(embassyId, serviceId)) |> buildRoute
                    service.Value.LastName, route.Value)
                |> createServiceButtonsGroup deps.Chat.Id deps.MessageId node.Value.Description
            | None ->
                $"You have no services available for '%s{serviceId.Value}'."
                |> NotFound
                |> Error
                |> async.Return)

let getUserServices embassyId =
    fun (deps: Root.Dependencies) ->
        embassyId
        |> tryCreateServiceRootId
        |> ResultAsync.wrap (fun serviceId -> deps |> getUserService embassyId serviceId)

let private getEmbassy' embassyId firstCall =
    fun (deps: Root.Dependencies) ->
        deps.tryFindEmbassyNode embassyId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) -> deps |> getServices embassyId
            | Some(AP.Node node) ->

                let messageId =
                    match firstCall with
                    | true -> None
                    | false -> deps.MessageId |> Some

                node.Children
                |> Seq.map (fun embassy ->
                    let embassyId = embassy.Id |> EmbassyId
                    let route = Get(Embassy embassyId) |> buildRoute
                    embassy.Value.LastName, route.Value)
                |> createEmbassyButtonsGroup deps.Chat.Id messageId node.Value.Description
            | None ->
                $"Embassy '%s{embassyId.Value}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
                |> async.Return)

let private getUserEmbassy' embassyId firstCall =
    fun (deps: Root.Dependencies) ->
        deps.tryFindEmbassyNode embassyId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) -> deps |> getUserServices embassyId
            | Some(AP.Node node) ->

                let messageId =
                    match firstCall with
                    | true -> None
                    | false -> deps.MessageId |> Some

                let userEmbassyIds = deps.Chat.Subscriptions |> Seq.map _.EmbassyId.NodeId

                node.Children
                |> Seq.filter (fun embassy -> userEmbassyIds |> Tree.NodeId.contains embassy.Id)
                |> Seq.map (fun embassy ->
                    let embassyId = embassy.Id |> EmbassyId
                    let route = Get(UserEmbassy embassyId) |> buildRoute
                    embassy.Value.LastName, route.Value)
                |> createEmbassyButtonsGroup deps.Chat.Id messageId node.Value.Description
            | None ->
                $"You have no embassies available for '%s{embassyId.Value}'."
                |> NotFound
                |> Error
                |> async.Return)

// Embassy API
let getEmbassy embassyId =
    fun (deps: Root.Dependencies) -> deps |> getEmbassy' embassyId false

let getEmbassies () =
    fun (deps: Root.Dependencies) ->
        Embassies.ROOT_ID
        |> EmbassyId.create
        |> ResultAsync.wrap (fun embassyId -> deps |> getEmbassy' embassyId true)

let getUserEmbassy embassyId =
    fun (deps: Root.Dependencies) -> deps |> getUserEmbassy' embassyId false

let getUserEmbassies () =
    fun (deps: Root.Dependencies) ->
        Embassies.ROOT_ID
        |> EmbassyId.create
        |> ResultAsync.wrap (fun embassyId -> deps |> getUserEmbassy' embassyId true)
