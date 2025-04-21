module EA.Telegram.Services.Embassies.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Embassies
open EA.Telegram.Services
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

let getEmbassy (embassyId: EmbassyId) =
    fun (deps: Embassies.Dependencies) ->
        deps.getEmbassyNode embassyId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) ->
                 deps.Request
                |> EA.Telegram.Dependencies.Services.Dependencies.create
                |> ResultAsync.wrap (Services.Query.getService embassyId)
            | Some(AP.Node node) ->
                node.Children
                |> Seq.map _.Value
                |> Seq.map (fun embassy ->
                    let route = Router.Embassies(Method.Get(Get.Embassy embassy.Id))
                    route.Value, embassy.Name)
                |> createButtonsGroup deps.ChatId deps.MessageId node.Value.Description
                |> Ok
                |> async.Return
            | None ->
                $"Embassy '%s{embassyId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
                |> async.Return)

let getEmbassies () =
    fun (deps: Embassies.Dependencies) -> deps |> getEmbassy (Embassies.ROOT_ID |> Graph.NodeIdValue |> EmbassyId)

let getUserEmbassy (embassyId: EmbassyId) =
    fun (deps: Embassies.Dependencies) ->
        deps.getUserEmbassyNode embassyId
        |> ResultAsync.bindAsync (function
            | Some(AP.Leaf _) ->
                deps.Request
                |> EA.Telegram.Dependencies.Services.Dependencies.create
                |> ResultAsync.wrap (Services.Query.getUserService embassyId)
            | Some(AP.Node node) ->
                node.Children
                |> Seq.map _.Value
                |> Seq.map (fun embassy ->
                    let route = Router.Embassies(Method.Get(Get.UserEmbassy embassy.Id))
                    route.Value, embassy.Name)
                |> createButtonsGroup deps.ChatId deps.MessageId node.Value.Description
                |> Ok
                |> async.Return
            | None ->
                $"You have no embassies of '%s{embassyId.ValueStr}' in your list."
                |> NotFound
                |> Error
                |> async.Return)

let getUserEmbassies () =
    fun (deps: Embassies.Dependencies) -> deps |> getUserEmbassy (Embassies.ROOT_ID |> Graph.NodeIdValue |> EmbassyId)
