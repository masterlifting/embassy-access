module EA.Telegram.Services.Consumer.Embassies.Service

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Dependencies.Consumer.Embassies
open EA.Telegram.Endpoints.Consumer
open EA.Telegram.Endpoints.Consumer.Embassies
open EA.Telegram.Endpoints.Consumer.Embassies.Request

module private Response =
    let private createButtons chatId msgIdOpt buttonGroupName columns data =
        match data |> Seq.length with
        | 0 -> Text.create "No data"
        | _ ->
            Buttons.create
                { Name = buttonGroupName |> Option.defaultValue "Choose what do you want to visit"
                  Columns = columns
                  Data = data |> Map.ofSeq }
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

module internal Get =

    let embassyService embassyId serviceId =
        fun (deps: Embassies.Dependencies) ->
            deps.getServiceNode serviceId
            |> ResultAsync.bindAsync (fun serviceNode ->
                match serviceNode.Children with
                | [] ->
                    match serviceNode.IdParts.Length > 1 with
                    | false ->
                        $"Embassy service '{serviceNode.ShortName}'"
                        |> NotSupported
                        |> Error
                        |> async.Return
                    | true ->
                        match serviceNode.IdParts[1].Value with
                        | "RUS" -> deps.RussianDeps |> Russian.Service.get embassyId serviceNode.Value
                        | _ ->
                            $"Embassy service '{serviceNode.ShortName}'"
                            |> NotSupported
                            |> Error
                            |> async.Return
                | children ->
                    children
                    |> Seq.map _.Value
                    |> Response.toEmbassyService deps.ChatId deps.MessageId serviceNode.Value.Description embassyId
                    |> Ok
                    |> async.Return)

    let userEmbassyService embassyId serviceId =
        fun (deps: Embassies.Dependencies) ->
            deps.getServiceNode serviceId
            |> ResultAsync.bindAsync (fun serviceNode ->
                match serviceNode.Children with
                | [] ->
                    match serviceNode.IdParts.Length > 1 with
                    | false ->
                        $"Embassy service '{serviceNode.ShortName}'"
                        |> NotSupported
                        |> Error
                        |> async.Return
                    | true ->
                        match serviceNode.IdParts[1].Value with
                        | "RUS" -> deps.RussianDeps |> Russian.Service.userGet embassyId serviceNode.Value
                        | _ ->
                            $"Embassy service '{serviceNode.ShortName}'"
                            |> NotSupported
                            |> Error
                            |> async.Return
                | children ->
                    children
                    |> Seq.map _.Value
                    |> Response.toEmbassyService deps.ChatId deps.MessageId serviceNode.Value.Description embassyId
                    |> Ok
                    |> async.Return)

    let embassyServices embassyId =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassyServiceGraph embassyId
            |> ResultAsync.map (fun node ->
                node.Children
                |> Seq.map _.Value
                |> Response.toEmbassyService deps.ChatId deps.MessageId node.Value.Description embassyId)

    let embassy embassyId =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassyNode embassyId
            |> ResultAsync.bindAsync (function
                | AP.Leaf value -> deps |> embassyServices value.Id
                | AP.Node node ->
                    node.Children
                    |> Seq.map _.Value
                    |> Response.toEmbassy deps.ChatId (Some deps.MessageId) node.Value.Description
                    |> Ok
                    |> async.Return)

    let embassies () =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassiesGraph ()
            |> ResultAsync.map (fun node ->
                node.Children
                |> Seq.map _.Value
                |> Response.toEmbassy deps.ChatId None node.Value.Description)
