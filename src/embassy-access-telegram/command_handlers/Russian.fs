module EA.Telegram.CommandHandler.Russian

open System
open Infrastructure
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram
open EA.Embassies.Russian.Domain

let service (country, serviceIdOpt) =
    fun (chatId, msgId) ->

        let inline createButtons (nodes: Graph.Node<ServiceInfo> seq) =
            nodes
            |> Seq.map (fun node -> (country, node.Id) |> Command.RussianService |> Command.set, node.Value.Name)
            |> Map
            |> fun data ->
                { Buttons.Name = "Какую услугу вы хотите получить?"
                  Columns = 1
                  Data = data }
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

                    let command =
                        (EA.Core.Domain.Russian country, serviceId) |> Command.ServiceGet |> Command.set

                    let message =
                        $"%s{command}{Environment.NewLine}Отправьте назад вышеуказанную комманду для получения услуги."

                    node.Value.Description
                    |> Option.map (fun instruction -> message + $"{Environment.NewLine}Инструкция: %s{instruction}")
                    |> Option.defaultValue message
                    |> Text.create (chatId, msgId |> Replace)
                | services -> services |> createButtons)
            |> async.Return
