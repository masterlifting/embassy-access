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

let private createButtons chatId name data =
    (chatId, New)
    |> Buttons.create
        { Name = name |> Option.defaultValue "Choose what do you want to get"
          Columns = 3
          Data = data |> Map.ofSeq }

let private createRequest chatId (node: Graph.Node<ServiceNode>) =
    match node.Children with
    | [] ->
        // match node.FullIds |> Seq.map _.Value |> Seq.tryHead with
        // | Some id ->
        //     match id with
        //     | "RU" ->

        node.FullId.Value |> NotSupported |> Error |> async.Return

    | children ->
        children
        |> Seq.map (fun node ->
            let request = node.FullId |> GetRequest.Id |> Get |> Router.Request.Services
            request.Route, node.ShortName)
        |> createButtons chatId node.Value.Description
        |> Ok
        |> async.Return

let private getService serviceId =
    fun (deps: Services.Dependencies) ->
        deps.ServiceGraph
        |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
        |> ResultAsync.bind (function
            | Some node -> Ok node
            | None -> $"ServiceId {serviceId.Value}" |> NotFound |> Error)
        |> ResultAsync.bindAsync (createRequest deps.ChatId)

let private getServices =
    fun (deps: Services.Dependencies) -> deps.ServiceGraph |> ResultAsync.bindAsync (createRequest deps.ChatId)

let consume request =
    fun (deps: Services.Dependencies) ->
        match request with
        | Request.Get getRequest ->
            match getRequest with
            | GetRequest.Id id -> deps |> getService id
            | GetRequest.All -> deps |> getServices
        | Request.Post postRequest -> "" |> NotSupported |> Error |> async.Return
        | Request.Delete deleteRequest ->
            match deleteRequest with
            | DeleteRequest.Id id -> "" |> NotSupported |> Error |> async.Return
