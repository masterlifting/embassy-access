[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Consumer.Services

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Routes.Services
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain

let private createButtons chatId msgIdOpt name data =
    (chatId, msgIdOpt |> Option.map Replace |> Option.defaultValue New)
    |> Buttons.create
        { Name = name |> Option.defaultValue "Choose what do you want to get"
          Columns = 3
          Data = data |> Map.ofSeq }

let private createServiceResponse (node: Graph.Node<ServiceNode>) =
    fun (deps: Services.Dependencies) ->
        match node.Children with
        | [] -> node.FullId.Value |> NotSupported |> Error |> async.Return
        | children ->
            children
            |> Seq.map (fun node ->
                let request = node.FullId |> GetRequest.Service |> Get |> EA.Telegram.Routes.Router.Request.Services
                request.Route, node.ShortName)
            |> createButtons deps.ChatId (Some deps.MessageId) node.Value.Description
            |> Ok
            |> async.Return
            
let private createEmbassyServiceResponse (embassyNode: Graph.Node<EmbassyNode>) (serviceNode: Graph.Node<ServiceNode>) =
    fun (deps: Services.Dependencies) ->
        match serviceNode.Children with
        | [] ->
            match serviceNode.FullIds |> Seq.map _.Value |> Seq.skip 1 |> Seq.tryHead with
            | Some value ->
                match value with
                | "RU" -> deps.RussianDeps |> Russian.getService embassyNode.Value serviceNode.Value
                | _ -> serviceNode.ShortName |> NotSupported |> Error |> async.Return
            | _ -> serviceNode.ShortName |> NotSupported |> Error |> async.Return
        | children ->
            children
            |> Seq.map (fun node ->
                let request = node.FullId |> GetRequest.Service |> Get |> EA.Telegram.Routes.Router.Request.Services
                request.Route, node.ShortName)
            |> createButtons deps.ChatId (Some deps.MessageId) serviceNode.Value.Description
            |> Ok
            |> async.Return

let internal getService serviceId =
    fun (deps: Services.Dependencies) ->
        deps.getService serviceId
        |> ResultAsync.map createServiceResponse
        |> ResultAsync.bindAsync (fun get -> deps |> get)

let internal getEmbassyServices (embassyNode: Graph.Node<EmbassyNode>) =
    fun (deps: Services.Dependencies) ->
        match embassyNode.FullIds |> Seq.skip 1 |> Seq.tryHead with
        | Some serviceId ->
            deps.getEmbassyService embassyNode.FullId
            |> ResultAsync.map (createEmbassyServiceResponse embassyNode)
            |> ResultAsync.bindAsync (fun get -> deps |> get)
        | None -> embassyNode.ShortName |> NotSupported |> Error |> async.Return
        
let internal getEmbassyService embassyId (serviceNode: Graph.Node<ServiceNode>) =
    fun (deps: Services.Dependencies) ->
        deps.getEmbassyService embassyId serviceNode.FullId
        |> ResultAsync.map (createEmbassyServiceResponse embassyNode)
        |> ResultAsync.bindAsync (fun get -> deps |> get)

let internal getServices =
    fun (deps: Services.Dependencies) ->
        deps.getServices
        |> ResultAsync.map createServiceResponse
        |> ResultAsync.bindAsync (fun get -> deps |> get)

let consume request =
    fun (deps: Core.Dependencies) ->
        Services.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Services -> deps |> getServices
                | Service id -> deps |> getService id
                | GetRequest.Services -> deps |> getServices
            | Request.Post postRequest -> "" |> NotSupported |> Error |> async.Return
            | Request.Delete deleteRequest ->
                match deleteRequest with
                | DeleteRequest.Id id -> "" |> NotSupported |> Error |> async.Return)
