[<RequireQualifiedAccess>]
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

let private createButtons chatId msgIdOpt name columns data =
    (chatId, msgIdOpt |> Option.map Replace |> Option.defaultValue New)
    |> Buttons.create
        { Name = name |> Option.defaultValue "Choose what do you want to visit"
          Columns = columns
          Data = data |> Map.ofSeq }

let private toEmbassyResponse chatId messageId name (embassies: EmbassyNode seq) =
    embassies
    |> Seq.map (fun embassy -> Core.Embassies(Get(Embassy(embassy.Id))).Route, embassy.Name |> Graph.split |> List.last)
    |> createButtons chatId messageId name 3

let private toEmbassyServiceResponse chatId messageId embassyId name (services: ServiceNode seq) =
    services
    |> Seq.map (fun service ->
        Core.Embassies(Get(EmbassyService(embassyId, service.Id))).Route, service.Name |> Graph.split |> List.last)
    |> createButtons chatId (Some messageId) name 1

module internal Get =
    let embassies (deps: Embassies.Dependencies) =
        deps.getEmbassies ()
        |> ResultAsync.map (toEmbassyResponse deps.ChatId None None)

    let embassyServices embassyId =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassyServiceNodes embassyId
            |> ResultAsync.map (
                Seq.map _.Value
                >> toEmbassyServiceResponse deps.ChatId deps.MessageId embassyId None
            )

    let embassyNodeServices (embassy: EmbassyNode) =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassyServiceNodes embassy.Id
            |> ResultAsync.map (
                Seq.map _.Value
                >> toEmbassyServiceResponse deps.ChatId deps.MessageId embassy.Id embassy.Description
            )

    let embassy embassyId =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassyNode embassyId
            |> ResultAsync.bindAsync (fun embassyNode ->
                match embassyNode.Children with
                | [] -> deps |> embassyNodeServices embassyNode.Value
                | children ->
                    children
                    |> Seq.map _.Value
                    |> toEmbassyResponse deps.ChatId (Some deps.MessageId) embassyNode.Value.Description
                    |> Ok
                    |> async.Return)

    let embassyService embassyId serviceId =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassyServiceNode embassyId serviceId
            |> ResultAsync.bindAsync (fun serviceNode ->
                match serviceNode.Children with
                | [] ->
                    match serviceNode.IdParts |> List.map _.Value with
                    | [ "SRV"; "RU"; _ ] ->
                        deps.RussianEmbassyDeps |> RussianEmbassy.getService embassyId serviceNode.Value
                    | _ -> serviceNode.ShortName |> NotSupported |> Error |> async.Return
                | children ->
                    children
                    |> Seq.map _.Value
                    |> toEmbassyServiceResponse deps.ChatId deps.MessageId embassyId serviceNode.Value.Description
                    |> Ok
                    |> async.Return)

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
