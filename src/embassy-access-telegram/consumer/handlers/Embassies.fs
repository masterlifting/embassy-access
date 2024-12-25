﻿[<RequireQualifiedAccess>]
module EA.Telegram.Consumer.Handlers.Embassies

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Consumer.Dependencies
open EA.Telegram.Consumer.Endpoints
open EA.Telegram.Consumer.Endpoints.Embassies

let private createButtons chatId msgIdOpt buttonGroupName columns data =
    (chatId, msgIdOpt |> Option.map Replace |> Option.defaultValue New)
    |> Buttons.create
        { Name = buttonGroupName |> Option.defaultValue "Choose what do you want to visit"
          Columns = columns
          Data = data |> Map.ofSeq }

let private toEmbassyResponse chatId messageId buttonGroupName (embassies: EmbassyNode seq) =
    embassies
    |> Seq.map (fun embassy -> Core.Embassies(Get(Embassy(embassy.Id))).Route, embassy.ShortName)
    |> createButtons chatId messageId buttonGroupName 3

let private toEmbassyServiceResponse chatId messageId buttonGroupName embassyId (services: ServiceNode seq) =
    services
    |> Seq.map (fun service -> Core.Embassies(Get(EmbassyService(embassyId, service.Id))).Route, service.ShortName)
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
                        | "RU" -> deps.RussianEmbassyDeps |> RussianEmbassy.getService embassyId serviceNode.Value
                        | _ ->
                            $"Embassy service '{serviceNode.ShortName}'"
                            |> NotSupported
                            |> Error
                            |> async.Return
                | children ->
                    children
                    |> Seq.map _.Value
                    |> toEmbassyServiceResponse deps.ChatId deps.MessageId serviceNode.Value.Description embassyId
                    |> Ok
                    |> async.Return)

    let embassyServices embassyId =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassyServices embassyId
            |> ResultAsync.map (toEmbassyServiceResponse deps.ChatId deps.MessageId None embassyId)

    let embassy embassyId =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassyNode embassyId
            |> ResultAsync.bindAsync (function
                | AP.Leaf value -> deps |> embassyServices value.Id
                | AP.Node node ->
                    node.Children
                    |> Seq.map _.Value
                    |> toEmbassyResponse deps.ChatId (Some deps.MessageId) node.Value.Description
                    |> Ok
                    |> async.Return)

    let embassies (deps: Embassies.Dependencies) =
        deps.getEmbassies ()
        |> ResultAsync.map (toEmbassyResponse deps.ChatId None None)

let toResponse request =
    fun (deps: Core.Dependencies) ->
        Embassies.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Embassies -> deps |> Get.embassies
                | Embassy embassyId -> deps |> Get.embassy embassyId
                | EmbassyServices embassyId -> deps |> Get.embassyServices embassyId
                | EmbassyService(embassyId, serviceId) -> deps |> Get.embassyService embassyId serviceId)
