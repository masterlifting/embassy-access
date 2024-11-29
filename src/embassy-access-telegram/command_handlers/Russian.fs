module EA.Telegram.CommandHandler.Russian

open System
open Infrastructure
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram
open EA.Embassies.Russian

let internal getService (embassyId, serviceIdOpt) =
    fun (cfg, chatId, msgId) ->

        let inline createButtons (nodes: Graph.Node<Domain.ServiceInfo> seq) =
            nodes
            |> Seq.map (fun node -> (embassyId, node.FullId) |> Command.GetService |> Command.set, node.ShortName)
            |> Map
            |> fun buttons ->
                { Buttons.Name = "Какую услугу вы хотите получить?"
                  Columns = 1
                  Data = buttons }
                |> Buttons.create (chatId, msgId |> Replace)

        cfg
        |> Settings.ServiceInfo.getGraph
        |> ResultAsync.bind (fun graph ->
            match serviceIdOpt with
            | None -> graph.Children |> createButtons |> Ok
            | Some serviceId ->
                graph
                |> Graph.BFS.tryFindById serviceId
                |> Option.map Ok
                |> Option.defaultValue ("Не могу найти выбранную услугу" |> NotFound |> Error)
                |> Result.map (fun node ->
                    match node.Children with
                    | [] ->

                        let command =
                            (embassyId, serviceId, "{вставить сюда}") |> Command.SetService |> Command.set

                        let doubleLine = Environment.NewLine + Environment.NewLine
                        let message = $"%s{command}%s{doubleLine}"

                        node.Value.Instruction
                        |> Option.map (fun instruction -> message + $"Инструкция:%s{doubleLine}%s{instruction}")
                        |> Option.defaultValue message
                        |> Text.create (chatId, msgId |> Replace)
                    | services -> services |> createButtons))

let internal setService (embassyId, serviceId, payload) =
    fun (cfg, chatId, msgId) ->

        cfg
        |> Settings.ServiceInfo.getGraph
        |> ResultAsync.bind (fun graph ->
            graph
            |> Graph.BFS.tryFindById serviceId
            |> Option.map Ok
            |> Option.defaultValue ("Не могу найти выбранную услугу" |> NotFound |> Error)
            |> Result.map (fun node ->
                node.Children
                |> Seq.tryFind (fun node -> node.Value.Instruction.IsSome)
                |> Option.map (fun node -> node.Value.Instruction.Value)
                |> Option.defaultValue "Услуга успешно выбрана"
                |> Text.create (chatId, New)))
