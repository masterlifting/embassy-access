module EA.Telegram.Services.Embassies.Query

open EA.Telegram.Domain
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Embassies
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Embassies

let private createButtonsGroup chatId msgIdOpt nameOpt columns data =
    let name = nameOpt |> Option.defaultValue "Choose from the list"

    match data |> Seq.length with
    | 0 -> Text.create $"No data for the '{name}'"
    | _ ->
        ButtonsGroup.create {
            Name = name
            Columns = columns
            Buttons =
                data
                |> Seq.map (fun (callback, name) -> callback |> CallbackData |> Button.create name)
                |> Set.ofSeq
        }
    |> Message.tryReplace msgIdOpt chatId

let private toButtonsGroup chatId messageId buttonGroupName (embassies: Embassy seq) =
    embassies
    |> Seq.map (fun embassy ->
        let route = Router.Embassies(Method.Get(Get.Embassy embassy.Id))
        route.Value, embassy.Name)
    |> createButtonsGroup chatId (Some messageId) buttonGroupName 3

let private toEmbassyService chatId messageId buttonGroupName embassyId (services: Service seq) =
    services
    |> Seq.map (fun service ->
        Router.Embassies(Method.Get(Get.UserEmbassies(embassyId, service.Id))).Value, service.Name)
    |> createButtonsGroup chatId (Some messageId) buttonGroupName 1

let getEmbassyService embassyId serviceId =
    fun (deps: Embassies.Dependencies) ->
        deps.getServiceNode serviceId
        |> ResultAsync.bindAsync (fun serviceNode ->
            match serviceNode.Children with
            | [] ->
                match serviceNode.Id.TryGetPart 1 with
                | None ->
                    $"Embassy service '{serviceNode.Value.Name}' is not supported."
                    |> NotSupported
                    |> Error
                    |> async.Return
                | Some countryId ->
                    match countryId.Value with
                    | Embassies.RUS -> (deps.Chat.Id, deps.MessageId) |> Russian.Query.get embassyId serviceNode.Value
                    | Embassies.ITA -> (deps.Chat.Id, deps.MessageId) |> Italian.Query.get embassyId serviceNode.Value
                    | _ ->
                        $"Embassy service '{serviceNode.Value.Name}' is not implemented. "
                        + NOT_IMPLEMENTED
                        |> NotImplemented
                        |> Error
                        |> async.Return
            | children ->
                children
                |> Seq.map _.Value
                |> toEmbassyService deps.Chat.Id deps.MessageId serviceNode.Value.Description embassyId
                |> Ok
                |> async.Return)

let getUserEmbassyService embassyId serviceId =
    fun (deps: Embassies.Dependencies) ->
        deps.getServiceNode serviceId
        |> ResultAsync.bindAsync (fun serviceNode ->
            match serviceNode.Children with
            | [] ->
                match serviceNode.Id.TryGetPart 1 with
                | None ->
                    $"Embassy service '{serviceNode.Value.Name}' is not supported."
                    |> NotSupported
                    |> Error
                    |> async.Return
                | Some countryId ->
                    match countryId.Value with
                    | Embassies.RUS -> deps.Russian |> Russian.Query.userGet embassyId serviceNode.Value
                    | Embassies.ITA -> deps.Italian |> Italian.Query.userGet embassyId serviceNode.Value
                    | _ ->
                        $"Embassy service '{serviceNode.Value.Name}' is not implemented. "
                        + NOT_IMPLEMENTED
                        |> NotImplemented
                        |> Error
                        |> async.Return
            | children ->
                children
                |> Seq.map _.Value
                |> toEmbassyService deps.Chat.Id deps.MessageId serviceNode.Value.Description embassyId
                |> Ok
                |> async.Return)

let getEmbassyServices embassyId =
    fun (deps: Embassies.Dependencies) ->
        deps.getEmbassyServiceGraph embassyId
        |> ResultAsync.map (fun node ->
            node.Children
            |> Seq.map _.Value
            |> toEmbassyService deps.Chat.Id deps.MessageId node.Value.Description embassyId)

let getEmbassy (embassyId: EmbassyId) =
    fun (deps: Embassies.Dependencies) ->
        deps.getEmbassyNode embassyId
        |> ResultAsync.bind (function
            | AP.Leaf value ->
                match
                    value.Id.Value
                    |> Graph.NodeId.split
                    |> Seq.skip 1
                    |> Seq.tryHead
                    |> Option.map _.Value
                with
                | Some countryId ->
                    match countryId with
                    | Embassies.RUS ->
                        let route = Router.Services(Services.Method.Russian(Services.Russian.Method.Get(Services.Get.Services value.Id)))
                    | Embassies.ITA ->
                        let route = Router.Services(Services.Method.Italian(Services.Italian.Method.Get(Services.Get.Services value.Id)))
                    | _ ->
                        $"Embassy '%s{value.Name}' is not implemented. " + NOT_IMPLEMENTED
                        |> NotImplemented
                        |> Error
                | None ->
                    $"Embassy '%s{value.Name}' is not implemented. " + NOT_IMPLEMENTED
                    |> NotImplemented
                    |> Error
            | AP.Node node ->
                node.Children
                |> Seq.map _.Value
                |> toButtonsGroup deps.Chat.Id deps.MessageId node.Value.Description
                |> Ok)

let getEmbassies () =
    fun (deps: Embassies.Dependencies) -> deps |> getEmbassy (Embassies.ROOT_ID |> Graph.NodeIdValue |> EmbassyId)

let getUserEmbassy (embassyId: EmbassyId) =
    fun (deps: Embassies.Dependencies) ->
        deps.getEmbassyNode embassyId
        |> ResultAsync.bindAsync (function
            | AP.Leaf value -> deps |> getUserEmbassyService value.Id
            | AP.Node node ->
                node.Children
                |> Seq.map _.Value
                |> toButtonsGroup deps.Chat.Id (Some deps.MessageId) node.Value.Description
                |> Ok
                |> async.Return)

let getUserEmbassies () =
    fun (deps: Embassies.Dependencies) ->
        deps.getEmbassiesGraph ()
        |> ResultAsync.map (fun node ->
            node.Children
            |> Seq.map _.Value
            |> toButtonsGroup deps.Chat.Id None node.Value.Description)
