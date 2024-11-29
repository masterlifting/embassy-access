module EA.Telegram.CommandHandler.Common

open System
open Infrastructure
open EA.Telegram
open EA.Core.Domain
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer

let private tryGetService (chatId, msgId) serviceIdOpt (embassyNode: Graph.Node<Embassy>) =
    match embassyNode.Names |> Seq.skip 1 |> Seq.tryHead with
    | Some embassyName ->
        match embassyName with
        | Constants.Embassy.RUSSIAN -> Russian.getService (embassyNode.FullId, serviceIdOpt) (chatId, msgId)
        | _ -> $"Service for {embassyNode.ShortName}" |> NotSupported |> Error |> async.Return
    | None -> embassyNode.ShortName |> NotFound |> Error |> async.Return

let private trySetService (chatId, msgId) serviceId payload (embassyNode: Graph.Node<Embassy>) =
    match embassyNode.Names |> Seq.skip 1 |> Seq.tryHead with
    | Some embassyName ->
        match embassyName with
        | Constants.Embassy.RUSSIAN -> Russian.setService (embassyNode.FullId, serviceId, payload) (chatId, msgId)
        | _ -> $"Service for {embassyNode.ShortName}" |> NotSupported |> Error |> async.Return
    | None -> embassyNode.ShortName |> NotFound |> Error |> async.Return

let getService (embassyId, serviceIdOpt) =
    fun (cfg, chatId, msgId) ->
        cfg
        |> EA.Core.Settings.Embassy.getGraph
        |> ResultAsync.bind (fun graph ->
            graph
            |> Graph.BFS.tryFindById embassyId
            |> Option.map Ok
            |> Option.defaultValue ($"EmbassyId {embassyId.Value}" |> NotFound |> Error))
        |> ResultAsync.bindAsync (tryGetService (chatId, msgId) serviceIdOpt)

let setService (embassyId, serviceId, payload) =
    fun (cfg, chatId, msgId) ->
        cfg
        |> EA.Core.Settings.Embassy.getGraph
        |> ResultAsync.bind (fun graph ->
            graph
            |> Graph.BFS.tryFindById embassyId
            |> Option.map Ok
            |> Option.defaultValue ($"EmbassyId {embassyId.Value}" |> NotFound |> Error))
        |> ResultAsync.bindAsync (trySetService (chatId, msgId) serviceId payload)


let getEmbassies embassyIdOpt =
    fun (cfg, chatId, msgId) ->

        let rec inline createButtons (nodes: Graph.Node<Embassy> seq) =
            nodes
            |> Seq.map (fun node -> node.FullId |> Command.GetEmbassy |> Command.set, node.ShortName)
            |> Map
            |> fun buttons ->

                let msgId =
                    match embassyIdOpt with
                    | Some _ -> Replace msgId
                    | None -> New

                { Buttons.Name = "Choose what do you want to visit"
                  Columns = 3
                  Data = buttons }
                |> Buttons.create (chatId, msgId)

        cfg
        |> EA.Core.Settings.Embassy.getGraph
        |> ResultAsync.bindAsync (fun graph ->
            match embassyIdOpt with
            | None -> graph.Children |> createButtons |> Ok |> async.Return
            | Some embassyId ->
                graph
                |> Graph.BFS.tryFindById embassyId
                |> Option.map Ok
                |> Option.defaultValue ($"EmbassyId {embassyId.Value}" |> NotFound |> Error)
                |> ResultAsync.wrap (fun node ->
                    match node.Children with
                    | [] -> node |> tryGetService (chatId, msgId) None
                    | nodes -> nodes |> createButtons |> Ok |> async.Return))

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
