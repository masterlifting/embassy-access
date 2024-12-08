module EA.Telegram.CommandHandler.Core

open System
open Infrastructure
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Core.Domain.Constants
open EA.Telegram.Initializer

let private tryGetService serviceIdOpt (embassyNode: Graph.Node<EmbassyGraph>) =
    fun (deps: ConsumerDeps) ->
        match embassyNode.Names |> Seq.skip 1 |> Seq.tryHead with
        | Some embassyName ->
            match embassyName with
            | EmbassyGraph.RUSSIAN ->
                Russian.Dependencies.GetService.create deps
                |> ResultAsync.wrap (Russian.Core.getService (embassyNode.FullId, serviceIdOpt))
            | _ -> $"Service for {embassyNode.ShortName}" |> NotSupported |> Error |> async.Return
        | None -> embassyNode.ShortName |> NotFound |> Error |> async.Return

let private trySetService serviceId payload (embassyNode: Graph.Node<EmbassyGraph>) =
    fun (deps: ConsumerDeps) ->
        match embassyNode.Names |> Seq.skip 1 |> Seq.tryHead with
        | Some embassyName ->
            match embassyName with
            | EmbassyGraph.RUSSIAN ->
                Russian.Dependencies.SetService.create deps
                |> ResultAsync.wrap (Russian.Core.setService (serviceId, embassyNode.Value, payload))
            | _ -> $"Service for {embassyNode.ShortName}" |> NotSupported |> Error |> async.Return
        | None -> embassyNode.ShortName |> NotFound |> Error |> async.Return

let getService (embassyId, serviceIdOpt) =
    fun (deps: Dependencies.GetService) ->
        deps.getEmbassiesGraph ()
        |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
        |> ResultAsync.bind (function
            | Some node -> Ok node
            | None -> $"EmbassyId {embassyId.Value}" |> NotFound |> Error)
        |> ResultAsync.map (tryGetService serviceIdOpt)
        |> ResultAsync.bindAsync (fun run -> run deps.ConsumerDeps)

let setService (embassyId, serviceId, payload) =
    fun (deps: Dependencies.SetService) ->
        deps.getEmbassiesGraph ()
        |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
        |> ResultAsync.bind (function
            | Some node -> Ok node
            | None -> $"EmbassyId {embassyId.Value}" |> NotFound |> Error)
        |> ResultAsync.map (trySetService serviceId payload)
        |> ResultAsync.bindAsync (fun run -> run deps.ConsumerDeps)

let getEmbassies embassyIdOpt =
    fun (deps: Dependencies.GetEmbassies) ->

        let inline createButtons buttonName (nodes: Graph.Node<EmbassyGraph> seq) =
            nodes
            |> Seq.map (fun node ->
                node.FullId |> EA.Telegram.Command.GetEmbassy |> EA.Telegram.Command.set, node.ShortName)
            |> Map
            |> fun buttons ->

                let msgId =
                    match embassyIdOpt with
                    | Some _ -> Replace deps.ConsumerDeps.MessageId
                    | None -> New

                { Buttons.Name = buttonName |> Option.defaultValue "Choose what do you want to visit"
                  Columns = 3
                  Data = buttons }
                |> Buttons.create (deps.ConsumerDeps.ChatId, msgId)

        deps.getEmbassiesGraph ()
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
                    | [] -> node |> tryGetService None |> (fun run -> run deps.ConsumerDeps)
                    | nodes -> nodes |> createButtons node.Value.Description |> Ok |> async.Return))

let getUserEmbassies embassyIdOpt =
    fun (deps: Dependencies.GetUserEmbassies) ->

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
                    | Some _ -> Replace deps.ConsumerDeps.MessageId
                    | None -> New

                { Buttons.Name = buttonName |> Option.defaultValue "Choose what do you want to visit"
                  Columns = 3
                  Data = buttons }
                |> Buttons.create (deps.ConsumerDeps.ChatId, msgId)

        deps.getChatEmbassies deps.ConsumerDeps.ChatId
        |> ResultAsync.bindAsync (fun embassies ->
            deps.getEmbassiesGraph ()
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
                        | [] -> embassyNode |> tryGetService None |> (fun run -> run deps.ConsumerDeps)
                        | nodes ->
                            nodes
                            |> createButtons embassyNode.Value.Description embassies
                            |> Ok
                            |> async.Return)))

let appointments (embassy: EmbassyGraph, appointments: Set<Appointment>) =
    fun chatId ->
        { Buttons.Name = $"Choose the appointment for '{embassy}'"
          Columns = 1
          Data =
            appointments
            |> Seq.map (fun appointment ->
                (embassy.Id, appointment.Id)
                |> EA.Telegram.Command.ChooseAppointments
                |> EA.Telegram.Command.set,
                appointment.Description)
            |> Map }
        |> Buttons.create (chatId, New)

let confirmation (embassy, confirmations: Set<Confirmation>) =
    fun chatId ->
        confirmations
        |> Seq.map (fun confirmation -> $"'{embassy}'. Confirmation: {confirmation.Description}")
        |> String.concat "\n"
        |> Text.create (chatId, New)
