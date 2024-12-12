[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Consumer.Core

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Handlers.Comsumer

let private tryGetService serviceIdOpt (embassyNode: Graph.Node<EmbassyGraph>) =
    fun (deps: Core.Dependencies) ->
        match embassyNode.Names |> Seq.skip 1 |> Seq.tryHead with
        | Some embassyName ->
            match embassyName with
            | "Russian" ->
                Russian.Dependencies.create deps
                |> ResultAsync.wrap (Russian.getService (embassyNode.FullId, serviceIdOpt))
            | _ -> $"Service for {embassyNode.ShortName}" |> NotSupported |> Error |> async.Return
        | None -> embassyNode.ShortName |> NotFound |> Error |> async.Return

let private trySetService serviceId payload (embassyNode: Graph.Node<EmbassyGraph>) =
    fun (deps: Core.Dependencies) ->
        match embassyNode.Names |> Seq.skip 1 |> Seq.tryHead with
        | Some embassyName ->
            match embassyName with
            | "Russian" ->
                Russian.Dependencies.create deps
                |> ResultAsync.wrap (Russian.setService (serviceId, embassyNode.Value, payload))
            | _ -> $"Service for {embassyNode.ShortName}" |> NotSupported |> Error |> async.Return
        | None -> embassyNode.ShortName |> NotFound |> Error |> async.Return

let getService (embassyId, serviceIdOpt) =
    fun (deps: Core.Dependencies) ->
        deps.getEmbassyGraph ()
        |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
        |> ResultAsync.bind (function
            | Some node -> Ok node
            | None -> $"EmbassyId {embassyId.Value}" |> NotFound |> Error)
        |> ResultAsync.map (tryGetService serviceIdOpt)
        |> ResultAsync.bindAsync (fun run -> run deps)

let setService (embassyId, serviceId, payload) =
    fun (deps: Core.Dependencies) ->
        deps.getEmbassyGraph ()
        |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
        |> ResultAsync.bind (function
            | Some node -> Ok node
            | None -> $"EmbassyId {embassyId.Value}" |> NotFound |> Error)
        |> ResultAsync.map (trySetService serviceId payload)
        |> ResultAsync.bindAsync (fun run -> run deps)

let getEmbassies embassyIdOpt =
    fun (deps: Core.Dependencies) ->

        let inline createButtons buttonName (nodes: Graph.Node<EmbassyGraph> seq) =
            nodes
            |> Seq.map (fun node ->
                node.FullId |> EA.Telegram.Command.GetEmbassy |> EA.Telegram.Command.set, node.ShortName)
            |> Map
            |> fun buttons ->

                let msgId =
                    match embassyIdOpt with
                    | Some _ -> Replace deps.MessageId
                    | None -> New

                { Buttons.Name = buttonName |> Option.defaultValue "Choose what do you want to visit"
                  Columns = 3
                  Data = buttons }
                |> Buttons.create (deps.ChatId, msgId)

        deps.getEmbassyGraph ()
        |> ResultAsync.bindAsync (fun graph ->
            match embassyIdOpt with
            | None -> graph.Children |> createButtons graph.Value.Description |> Ok |> async.Return
            | Some embassyId ->
                graph
                |> Graph.BFS.tryFindById embassyId
                |> Option.map Ok
                |> Option.defaultValue ($"EmbassyId {embassyId.Value}" |> NotFound |> Error)
                |> ResultAsync.wrap (fun node ->
                    match node.Children with
                    | [] -> node |> tryGetService None |> (fun run -> run deps)
                    | nodes -> nodes |> createButtons node.Value.Description |> Ok |> async.Return))

let getUserEmbassies embassyIdOpt =
    fun (deps: Core.Dependencies) ->

        let inline createButtons buttonName (embassies: EmbassyGraph seq) (nodes: Graph.Node<EmbassyGraph> seq) =
            let embassyIds = embassies |> Seq.map _.Id |> Set

            nodes
            |> Seq.filter (fun node -> embassyIds.Contains node.FullId)
            |> Seq.map (fun node ->
                node.FullId |> EA.Telegram.Command.GetEmbassy |> EA.Telegram.Command.set, node.ShortName)
            |> Map
            |> fun buttons ->

                let msgId =
                    match embassyIdOpt with
                    | Some _ -> Replace deps.MessageId
                    | None -> New

                { Buttons.Name = buttonName |> Option.defaultValue "Choose what do you want to visit"
                  Columns = 3
                  Data = buttons }
                |> Buttons.create (deps.ChatId, msgId)

        deps.getChatEmbassies ()
        |> ResultAsync.bindAsync (fun embassies ->
            deps.getEmbassyGraph ()
            |> ResultAsync.bindAsync (fun graph ->
                match embassyIdOpt with
                | None ->
                    graph.Children
                    |> createButtons graph.Value.Description embassies
                    |> Ok
                    |> async.Return
                | Some embassyId ->
                    graph
                    |> Graph.BFS.tryFindById embassyId
                    |> Option.map Ok
                    |> Option.defaultValue ($"EmbassyId {embassyId.Value}" |> NotFound |> Error)
                    |> ResultAsync.wrap (fun embassyNode ->
                        match embassyNode.Children with
                        | [] -> embassyNode |> tryGetService None |> (fun run -> run deps)
                        | nodes ->
                            nodes
                            |> createButtons embassyNode.Value.Description embassies
                            |> Ok
                            |> async.Return)))
