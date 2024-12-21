[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Consumer.Services

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Routes.Services
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Routes

let private createButtons chatId msgIdOpt name data =
    (chatId, msgIdOpt |> Option.map Replace |> Option.defaultValue New)
    |> Buttons.create
        { Name = name |> Option.defaultValue "Choose what do you want to get"
          Columns = 3
          Data = data |> Map.ofSeq }

let private createRequest chatId msgId (node: Graph.Node<ServiceNode>) =
    fun (deps: Services.Dependencies) ->
        match node.Children with
        | [] -> node.FullId.Value |> NotSupported |> Error |> async.Return
        | children ->
            children
            |> Seq.map (fun node ->
                let request = node.FullId |> GetRequest.Id |> Get |> Router.Request.Services
                request.Route, node.ShortName)
            |> createButtons chatId (Some msgId) node.Value.Description
            |> Ok
            |> async.Return

let private createEmbassyRequest embassy (node: Graph.Node<ServiceNode>) =
    fun (deps: Services.Dependencies) ->
        match node.Children with
        | [] ->
            match node.FullIds |> Seq.map _.Value |> Seq.skip 1 |> Seq.head with
            | "RU" ->
                deps.RussianServicesDeps
                |> EA.Telegram.Handlers.Comsumer.Russian.sendInstruction node.Value embassy
                |> Ok
                |> async.Return
            | _ -> node.ShortName |> NotSupported |> Error |> async.Return
        | children ->
            children
            |> Seq.map (fun node ->
                let request = node.FullId |> GetRequest.Id |> Get |> Router.Request.Services
                request.Route, node.ShortName)
            |> createButtons deps.ChatId (Some deps.MessageId) node.Value.Description
            |> Ok
            |> async.Return

let internal getService serviceId =
    fun (deps: Services.Dependencies) ->
        deps.ServiceGraph
        |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
        |> ResultAsync.bind (function
            | Some node -> Ok node
            | None -> $"ServiceId {serviceId.Value}" |> NotFound |> Error)
        |> ResultAsync.map (createRequest deps.ChatId deps.MessageId)
        |> ResultAsync.bindAsync (fun f -> deps |> f)

let internal getEmbassyService serviceId embassy =
    fun (deps: Services.Dependencies) ->
        deps.ServiceGraph
        |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
        |> ResultAsync.bind (function
            | Some node -> Ok node
            | None -> $"ServiceId {serviceId.Value}" |> NotFound |> Error)
        |> ResultAsync.map (createEmbassyRequest embassy)
        |> ResultAsync.bindAsync (fun f -> deps |> f)

let internal getServices =
    fun (deps: Services.Dependencies) ->
        deps.ServiceGraph
        |> ResultAsync.map (createRequest deps.ChatId deps.MessageId)
        |> ResultAsync.bindAsync (fun f -> deps |> f)

let consume request =
    fun (deps: Core.Dependencies) ->
        Services.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Request.Get getRequest ->
                match getRequest with
                | GetRequest.Id id -> deps |> getService id
                | GetRequest.All -> deps |> getServices
            | Request.Post postRequest -> "" |> NotSupported |> Error |> async.Return
            | Request.Delete deleteRequest ->
                match deleteRequest with
                | DeleteRequest.Id id -> "" |> NotSupported |> Error |> async.Return)
