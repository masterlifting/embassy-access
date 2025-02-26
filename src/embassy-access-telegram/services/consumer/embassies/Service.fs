module EA.Telegram.Services.Consumer.Embassies.Service

open System
open EA.Telegram.Domain
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Endpoints
open EA.Telegram.Endpoints.Embassies
open EA.Telegram.Endpoints.Embassies.Request
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Dependencies.Consumer.Embassies

module private Response =
    let private createButtons chatId msgIdOpt buttonGroupName columns data =
        match data |> Seq.length with
        | 0 -> Text.create "No data"
        | _ ->
            ButtonsGroup.create
                { Name = buttonGroupName |> Option.defaultValue "Choose what do you want"
                  Columns = columns
                  Items = data |> Map.ofSeq }
        |> fun send -> (chatId, msgIdOpt |> Option.map Replace |> Option.defaultValue New) |> send

    let toEmbassy chatId messageId buttonGroupName (embassies: EmbassyNode seq) =
        embassies
        |> Seq.map (fun embassy -> Request.Embassies(Get(Get.Embassy(embassy.Id))).Value, embassy.ShortName)
        |> createButtons chatId messageId buttonGroupName 3

    let toEmbassyService chatId messageId buttonGroupName embassyId (services: ServiceNode seq) =
        services
        |> Seq.map (fun service ->
            Request.Embassies(Get(Get.EmbassyService(embassyId, service.Id))).Value, service.ShortName)
        |> createButtons chatId (Some messageId) buttonGroupName 1

module internal Query =

    let getEmbassyService embassyId serviceId =
        fun (deps: Embassies.Dependencies) ->
            deps.getServiceNode serviceId
            |> ResultAsync.bindAsync (fun serviceNode ->
                match serviceNode.Children with
                | [] ->
                    match serviceNode.Id.TryGetPart 1 with
                    | None ->
                        $"Embassy service '{serviceNode.ShortName}'"
                        |> NotSupported
                        |> Error
                        |> async.Return
                    | Some countryId ->
                        match countryId.Value with
                        | Constants.RUSSIAN_NODE_ID ->
                            deps.RussianDeps |> Russian.Service.get embassyId serviceNode.Value
                        | _ ->
                            $"Embassy service '{serviceNode.ShortName}'"
                            |> NotSupported
                            |> Error
                            |> async.Return
                | children ->
                    children
                    |> Seq.map _.Value
                    |> Response.toEmbassyService deps.Chat.Id deps.MessageId serviceNode.Value.Description embassyId
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
                        $"Embassy service '{serviceNode.ShortName}'"
                        |> NotSupported
                        |> Error
                        |> async.Return
                    | Some countryId ->
                        match countryId.Value with
                        | Constants.RUSSIAN_NODE_ID ->
                            deps.RussianDeps |> Russian.Service.userGet embassyId serviceNode.Value
                        | _ ->
                            $"Embassy service '{serviceNode.ShortName}'"
                            |> NotSupported
                            |> Error
                            |> async.Return
                | children ->
                    children
                    |> Seq.map _.Value
                    |> Response.toEmbassyService deps.Chat.Id deps.MessageId serviceNode.Value.Description embassyId
                    |> Ok
                    |> async.Return)

    let getEmbassyServices embassyId =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassyServiceGraph embassyId
            |> ResultAsync.map (fun node ->
                node.Children
                |> Seq.map _.Value
                |> Response.toEmbassyService deps.Chat.Id deps.MessageId node.Value.Description embassyId)

    let getEmbassy embassyId =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassyNode embassyId
            |> ResultAsync.bindAsync (function
                | AP.Leaf value -> deps |> getEmbassyServices value.Id
                | AP.Node node ->
                    node.Children
                    |> Seq.map _.Value
                    |> Response.toEmbassy deps.Chat.Id (Some deps.MessageId) node.Value.Description
                    |> Ok
                    |> async.Return)

    let getEmbassies () =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassiesGraph ()
            |> ResultAsync.map (fun node ->
                node.Children
                |> Seq.map _.Value
                |> Response.toEmbassy deps.Chat.Id None node.Value.Description)
