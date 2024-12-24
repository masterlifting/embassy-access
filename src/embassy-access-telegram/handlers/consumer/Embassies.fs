[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Consumer.Embassies

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Routes
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Routes.Embassies

let private createButtons chatId msgIdOpt name data =
    (chatId, msgIdOpt |> Option.map Replace |> Option.defaultValue New)
    |> Buttons.create
        { Name = name |> Option.defaultValue "Choose what do you want to visit"
          Columns = 3
          Data = data |> Map.ofSeq }

let private createRequest (node: Graph.Node<EmbassyNode>) =
    fun (deps: Embassies.Dependencies) ->
        match node.Children with
        | [] -> deps.ServicesDeps |> Services.getEmbassyService node
        | children ->
            children
            |> Seq.map (fun node ->
                let request = node.FullId |> GetRequest.Embassy |> Get |> Router.Request.Embassies
                request.Route, node.ShortName)
            |> createButtons deps.ChatId (Some deps.MessageId) node.Value.Description
            |> Ok
            |> async.Return

module internal Get =
    let embassies =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassies ()
            |> ResultAsync.map createRequest
            |> ResultAsync.bindAsync (fun get -> deps |> get)

    let embassy embassyId =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassy embassyId
            |> ResultAsync.map createRequest
            |> ResultAsync.bindAsync (fun get -> deps |> get)

    let embassyServices embassyId =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassyServices embassyId
            |> ResultAsync.map (fun services ->
                services
                |> Seq.map (fun node ->
                    let request = Router.Embassies(Get(EmbassyService(embassyId, node.FullId)))
                    request.Route, node.ShortName)
                |> createButtons deps.ChatId (Some deps.MessageId) None)

    let embassyService embassyId serviceId =
        fun (deps: Embassies.Dependencies) ->
            deps.getEmbassyService embassyId serviceId
            |> ResultAsync.bindAsync (fun serviceNode ->
                match serviceNode.Children with
                | [] -> serviceNode.FullName |> NotSupported |> Error |> async.Return
                | children ->
                    children
                    |> Seq.map (fun node ->
                        let request = Router.Embassies(Get(EmbassyService(embassyId, node.FullId)))
                        request.Route, node.ShortName)
                    |> createButtons deps.ChatId (Some deps.MessageId) serviceNode.Value.Description
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
