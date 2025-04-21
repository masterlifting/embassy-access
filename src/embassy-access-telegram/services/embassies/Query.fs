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
open EA.Telegram.Dependencies.Embassies

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

let getEmbassy embassyId =
    fun (deps: Embassies.Dependencies) ->
        deps.getEmbassyNode embassyId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) ->
                deps.Request
                |> Services.Dependencies.create deps.Chat
                |> ResultAsync.wrap (Services.Query.getService embassyId)
            | Some(AP.Node node) ->
                node.Children
                |> Seq.map _.Value
                |> Seq.map (fun embassy ->
                    let route = Router.Embassies(Method.Get(Get.Embassy embassy.Id))
                    route.Value, embassy.Name)
                |> createButtonsGroup deps.Chat.Id deps.MessageId node.Value.Description
            | None ->
                $"Embassy '%s{embassyId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
                |> async.Return)

let getEmbassies () =
    fun (deps: Embassies.Dependencies) -> deps |> getEmbassy (Embassies.ROOT_ID |> Graph.NodeIdValue |> EmbassyId)

let getUserEmbassy embassyId =
    fun (deps: Embassies.Dependencies) ->
        deps.getEmbassyNode embassyId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) ->
                deps.Request
                |> Services.Dependencies.create deps.Chat
                |> ResultAsync.wrap (Services.Query.getUserService embassyId)
            | Some(AP.Node node) ->

                let userEmbassyIds = deps.Chat.Subscriptions |> Seq.map _.EmbassyId.Value

                node.Children
                |> Seq.map _.Value
                |> Seq.filter (fun embassy -> embassy.Id.Value.In userEmbassyIds)
                |> Seq.map (fun embassy ->
                    let route = Router.Embassies(Method.Get(Get.UserEmbassy embassy.Id))
                    route.Value, embassy.Name)
                |> createButtonsGroup deps.Chat.Id deps.MessageId node.Value.Description
            | None ->
                $"You have no embassies of '%s{embassyId.ValueStr}' in your list."
                |> NotFound
                |> Error
                |> async.Return)

let getUserEmbassies () =
    fun (deps: Embassies.Dependencies) -> deps |> getUserEmbassy (Embassies.ROOT_ID |> Graph.NodeIdValue |> EmbassyId)
