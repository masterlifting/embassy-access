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
                let request = node.FullId |> GetRequest.Id |> Get |> Router.Request.Embassies
                request.Route, node.ShortName)
            |> createButtons deps.ChatId (Some deps.MessageId) node.Value.Description
            |> Ok
            |> async.Return

let private getEmbassy embassyId =
    fun (deps: Embassies.Dependencies) ->
        deps.EmbassyGraph
        |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
        |> ResultAsync.bind (function
            | Some node -> Ok node
            | None -> $"EmbassyId {embassyId.Value}" |> NotFound |> Error)
        |> ResultAsync.map createRequest
        |> ResultAsync.bindAsync (fun get -> deps |> get)

let private getEmbassies =
    fun (deps: Embassies.Dependencies) ->
        deps.EmbassyGraph
        |> ResultAsync.map createRequest
        |> ResultAsync.bindAsync (fun get -> deps |> get)

let consume request =
    fun (deps: Core.Dependencies) ->
        Embassies.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Request.Get getRequest ->
                match getRequest with
                | GetRequest.Id id -> deps |> getEmbassy id
                | GetRequest.All -> deps |> getEmbassies
                | GetRequest.Services(embassyId, services) -> "" |> NotSupported |> Error |> async.Return)
