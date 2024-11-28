module EA.Telegram.CommandHandler.Russian

open System
open Infrastructure
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram
open EA.Embassies.Russian.Domain

let service (embassyId, serviceIdOpt) =
    fun (chatId, msgId) ->

        let createButtons (nodes: Graph.Node<ServiceInfo> seq) =
            nodes
            |> Seq.map (fun node -> (embassyId, node.Id) |> Command.GetService |> Command.set, node.ShortName)
            |> Map
            |> fun buttons ->
                { Buttons.Name = "Какую услугу вы хотите получить?"
                  Columns = 1
                  Data = buttons }
                |> Buttons.create (chatId, msgId |> Replace)

        match serviceIdOpt with
        | None -> Service.GRAPH.Children |> createButtons |> Ok |> async.Return
        | Some serviceId ->
            Service.GRAPH
            |> Graph.BFS.tryFindById serviceId
            |> Option.map Ok
            |> Option.defaultValue ("Не могу найти выбранную услугу" |> NotFound |> Error)
            |> Result.map (fun node ->
                match node.Children with
                | [] ->

                    let command = (embassyId, serviceId) |> Command.GetService |> Command.set

                    let message =
                        $"%s{command}%s{Environment.NewLine}Отправьте назад вышеуказанную комманду для получения услуги."

                    node.Value.Description
                    |> Option.map (fun instruction -> message + $"%s{Environment.NewLine}Инструкция: %s{instruction}")
                    |> Option.defaultValue message
                    |> Text.create (chatId, msgId |> Replace)
                | services -> services |> createButtons)
            |> async.Return
