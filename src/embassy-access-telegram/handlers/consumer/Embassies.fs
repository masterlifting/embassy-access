[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Consumer.Embassies

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Routes.Embassies
open System
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Routes

let private createButtons chatId name data =
    (chatId, New)
    |> Buttons.create
        { Name = name |> Option.defaultValue "Choose what do you want to visit"
          Columns = 3
          Data = data |> Map.ofSeq }

let private createRequest chatId (node: Graph.Node<EmbassyNode>) =
    match node.Children with
    | [] -> node.FullId.Value |> NotSupported |> Error |> async.Return
    | children ->
        children
        |> Seq.map (fun node ->
            let request = node.FullId |> GetRequest.Id |> Get |> Router.Request.Embassies
            request.Route, node.ShortName)
        |> createButtons chatId node.Value.Description
        |> Ok
        |> async.Return

let private getEmbassy embassyId =
    fun (deps: Embassies.Dependencies) ->
        deps.EmbassyGraph
        |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
        |> ResultAsync.bind (function
            | Some node -> Ok node
            | None -> $"EmbassyId {embassyId.Value}" |> NotFound |> Error)
        |> ResultAsync.bindAsync (createRequest deps.ChatId)

let private getEmbassies =
    fun (deps: Embassies.Dependencies) -> deps.EmbassyGraph |> ResultAsync.bindAsync (createRequest deps.ChatId)

let consume request =
    fun (deps: Embassies.Dependencies) ->
        match request with
        | Request.Get getRequest ->
            match getRequest with
            | GetRequest.Id id -> deps |> getEmbassy id
            | GetRequest.All -> deps |> getEmbassies
            | GetRequest.Services(embassyId, services) -> "" |> NotSupported |> Error |> async.Return
