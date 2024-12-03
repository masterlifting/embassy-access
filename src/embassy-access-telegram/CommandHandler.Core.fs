module EA.Telegram.CommandHandler.Core

open System
open Infrastructure
open EA.Telegram
open EA.Core.Domain
open EA.Core.Domain.Constants
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer

let private tryGetService serviceIdOpt (embassyNode: Graph.Node<Embassy>) =
    fun (cfg, chatId, msgId) ->
        match embassyNode.Names |> Seq.skip 1 |> Seq.tryHead with
        | Some embassyName ->
            match embassyName with
            | Embassy.RUSSIAN ->
                Russian.getService (embassyNode.FullId, serviceIdOpt)
                |> fun createData -> createData (cfg, chatId, msgId)
            | _ -> $"Service for {embassyNode.ShortName}" |> NotSupported |> Error |> async.Return
        | None -> embassyNode.ShortName |> NotFound |> Error |> async.Return

let private trySetService serviceId payload (embassyNode: Graph.Node<Embassy>) =
    fun (cfg, chatId, msgId, ct) ->
        match embassyNode.Names |> Seq.skip 1 |> Seq.tryHead with
        | Some embassyName ->
            match embassyName with
            | Embassy.RUSSIAN ->
                Russian.setService (serviceId, embassyNode.Value, payload)
                |> fun createData -> createData (cfg, chatId, msgId, ct)
            | _ -> $"Service for {embassyNode.ShortName}" |> NotSupported |> Error |> async.Return
        | None -> embassyNode.ShortName |> NotFound |> Error |> async.Return

let getService (embassyId, serviceIdOpt) =
    fun (cfg, chatId, msgId) ->
        cfg
        |> EA.Core.Settings.Embassy.getGraph
        |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
        |> ResultAsync.bind (function
            | Some node -> Ok node
            | None -> $"EmbassyId {embassyId.Value}" |> NotFound |> Error)
        |> ResultAsync.map (tryGetService serviceIdOpt)
        |> ResultAsync.bindAsync (fun createData -> createData (cfg, chatId, msgId))

let setService (embassyId, serviceId, payload) =
    fun (cfg, chatId, msgId, ct) ->
        cfg
        |> EA.Core.Settings.Embassy.getGraph
        |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
        |> ResultAsync.bind (function
            | Some node -> Ok node
            | None -> $"EmbassyId {embassyId.Value}" |> NotFound |> Error)
        |> ResultAsync.map (trySetService serviceId payload)
        |> ResultAsync.bindAsync (fun createData -> createData (cfg, chatId, msgId, ct))

let getEmbassies embassyIdOpt =
    fun (cfg, chatId, msgId) ->

        let inline createButtons buttonName (nodes: Graph.Node<Embassy> seq) =
            nodes
            |> Seq.map (fun node -> node.FullId |> Command.GetEmbassy |> Command.set, node.ShortName)
            |> Map
            |> fun buttons ->

                let msgId =
                    match embassyIdOpt with
                    | Some _ -> Replace msgId
                    | None -> New

                { Buttons.Name = buttonName |> Option.defaultValue "Choose what do you want to visit"
                  Columns = 3
                  Data = buttons }
                |> Buttons.create (chatId, msgId)

        cfg
        |> EA.Core.Settings.Embassy.getGraph
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
                    | [] ->
                        node
                        |> tryGetService None
                        |> fun createData -> createData (cfg, chatId, msgId)
                    | nodes -> nodes |> createButtons node.Value.Description |> Ok |> async.Return))

let getUserEmbassies embassyIdOpt =
    fun (cfg, chatId, msgId, ct, deps: CommandHandler.Domain.GetUserEmbassies) ->

        let inline createButtons buttonName (embassies: Embassy seq) (nodes: Graph.Node<Embassy> seq) =
            let embassyIds = embassies |> Seq.map _.Id |> Set

            nodes
            |> Seq.filter (fun node -> embassyIds.Contains node.FullId)
            |> Seq.map (fun node -> node.FullId |> Command.GetEmbassy |> Command.set, node.ShortName)
            |> Map
            |> fun buttons ->

                let msgId =
                    match embassyIdOpt with
                    | Some _ -> Replace msgId
                    | None -> New

                { Buttons.Name = buttonName |> Option.defaultValue "Choose what do you want to visit"
                  Columns = 3
                  Data = buttons }
                |> Buttons.create (chatId, msgId)

        deps.initializeChatStorage()
        |> ResultAsync.wrap(deps.getChat chatId)
        |> ResultAsync.bind (function
            | Some chat -> Ok chat
            | None -> $"Data for {chatId}" |> NotFound |> Error)
        |> ResultAsync.bindAsync deps.getChatEmbassies
        |> ResultAsync.bindAsync (fun embassies ->
            cfg
            |> EA.Core.Settings.Embassy.getGraph
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
                    |> ResultAsync.wrap (fun node ->
                        match node.Children with
                        | [] ->
                            node
                            |> tryGetService None
                            |> fun createData -> createData (cfg, chatId, msgId)
                        | nodes -> nodes |> createButtons node.Value.Description embassies |> Ok |> async.Return)))

let appointments (embassy: Embassy, appointments: Set<Appointment>) =
    fun chatId ->
        { Buttons.Name = $"Choose the appointment for '{embassy}'"
          Columns = 1
          Data =
            appointments
            |> Seq.map (fun appointment ->
                (embassy.Id, appointment.Id) |> Command.ChooseAppointments |> Command.set, appointment.Description)
            |> Map }
        |> Buttons.create (chatId, New)

let confirmation (embassy, confirmations: Set<Confirmation>) =
    fun chatId ->
        confirmations
        |> Seq.map (fun confirmation -> $"'{embassy}'. Confirmation: {confirmation.Description}")
        |> String.concat "\n"
        |> Text.create (chatId, New)
