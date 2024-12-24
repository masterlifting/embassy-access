[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Consumer.Embassies

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Routes.Embassies

let private createButtons chatId msgIdOpt name data =
    (chatId, msgIdOpt |> Option.map Replace |> Option.defaultValue New)
    |> Buttons.create
        { Name = name |> Option.defaultValue "Choose what do you want to visit"
          Columns = 3
          Data = data |> Map.ofSeq }

let private toEmbassyResponse chatId messageId name (embassies: Graph.Node<EmbassyNode> seq) =
    embassies
    |> Seq.map (fun embassy -> EA.Telegram.Routes.Router.Embassies(Get(Embassy(embassy.Id))).Route, embassy.ShortName)
    |> createButtons chatId (Some messageId) name

let private toEmbassyServiceResponse chatId messageId embassyId name (services: Graph.Node<ServiceNode> seq) =
    services
    |> Seq.map (fun service ->
        EA.Telegram.Routes.Router
            .Embassies(Get(EmbassyService(embassyId, service.Id)))
            .Route,
        service.ShortName)
    |> createButtons chatId (Some messageId) name

module internal Get =
    let embassies =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassies ()
            |> ResultAsync.map (toEmbassyResponse deps.ChatId deps.MessageId None)

    let embassyServices embassyId =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassyServices embassyId
            |> ResultAsync.map (toEmbassyServiceResponse deps.ChatId deps.MessageId embassyId None)

    let embassyNodeServices (embassyNode: Graph.Node<EmbassyNode>) =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassyServices embassyNode.Id
            |> ResultAsync.map (
                toEmbassyServiceResponse deps.ChatId deps.MessageId embassyNode.Id embassyNode.Value.Description
            )

    let embassy embassyId =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassy embassyId
            |> ResultAsync.bindAsync (fun embassyNode ->
                match embassyNode.Children with
                | [] -> deps |> embassyNodeServices embassyNode
                | children ->
                    children
                    |> toEmbassyResponse deps.ChatId deps.MessageId embassyNode.Value.Description
                    |> Ok
                    |> async.Return)

    let embassyService embassyId serviceId =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassyService embassyId serviceId
            |> ResultAsync.bindAsync (fun serviceNode ->
                match serviceNode.Children with
                | [] ->
                    match serviceNode.IdParts |> List.map _.Value with
                    | [ "SRV"; "RU"; _ ] ->
                        deps.RussianServiceDeps
                        |> EA.Telegram.Handlers.Consumer.Services.Russian.getService embassyId serviceNode.Value
                    | _ -> serviceNode.ShortName |> NotSupported |> Error |> async.Return
                | children ->
                    children
                    |> toEmbassyServiceResponse deps.ChatId deps.MessageId embassyId serviceNode.Value.Description
                    |> Ok
                    |> async.Return)

let consume request =
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
