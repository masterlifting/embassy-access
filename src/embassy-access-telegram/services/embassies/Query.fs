module EA.Telegram.Services.Embassies.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Embassies
open EA.Telegram.Services
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Services
open EA.Telegram.Dependencies.Embassies

let private createButtonsGroup chatId messageId name buttons =
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

let private getEmbassy' embassyId firstCall =
    fun (deps: Embassies.Dependencies) ->
        deps.getEmbassyNode embassyId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) ->
                deps.Request
                |> Services.Dependencies.create deps.Chat
                |> ResultAsync.wrap (Services.Query.getServices embassyId)
            | Some(AP.Node node) ->

                let messageId =
                    match firstCall with
                    | true -> None
                    | false -> deps.MessageId |> Some

                node.Children
                |> Seq.map _.Value
                |> Seq.map (fun embassy ->
                    let route = Router.Embassies(Method.Get(Get.Embassy embassy.Id))
                    embassy.LastName, route.Value)
                |> createButtonsGroup deps.Chat.Id messageId node.Value.Description
            | None ->
                $"Embassy '%s{embassyId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
                |> async.Return)

let private getUserEmbassy' embassyId firstCall =
    fun (deps: Embassies.Dependencies) ->
        deps.getEmbassyNode embassyId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) ->
                deps.Request
                |> Services.Dependencies.create deps.Chat
                |> ResultAsync.wrap (Services.Query.getUserServices embassyId)
            | Some(AP.Node node) ->

                let messageId =
                    match firstCall with
                    | true -> None
                    | false -> deps.MessageId |> Some

                let userEmbassyIds = deps.Chat.Subscriptions |> Seq.map _.EmbassyId.Value

                node.Children
                |> Seq.map _.Value
                |> Seq.filter (fun embassy -> embassy.Id.Value.IsInSeq userEmbassyIds)
                |> Seq.map (fun embassy ->
                    let route = Router.Embassies(Method.Get(Get.UserEmbassy embassy.Id))
                    embassy.LastName, route.Value)
                |> createButtonsGroup deps.Chat.Id messageId node.Value.Description
            | None ->
                $"You have no embassies available for '%s{embassyId.ValueStr}'."
                |> NotFound
                |> Error
                |> async.Return)

let getEmbassy embassyId =
    fun (deps: Embassies.Dependencies) -> deps |> getEmbassy' embassyId false

let getEmbassies () =
    fun (deps: Embassies.Dependencies) ->
        let embassyId = Embassies.ROOT_ID |> Graph.NodeIdValue |> EmbassyId
        deps |> getEmbassy' embassyId true

let getUserEmbassy embassyId =
    fun (deps: Embassies.Dependencies) -> deps |> getUserEmbassy' embassyId false

let getUserEmbassies () =
    fun (deps: Embassies.Dependencies) ->
        let embassyId = Embassies.ROOT_ID |> Graph.NodeIdValue |> EmbassyId
        deps |> getUserEmbassy' embassyId true
