module EA.Telegram.CommandHandler.Russian

open System
open Infrastructure
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram
open EA.Embassies.Russian.Domain

let internal getService (embassyId, serviceIdOpt) =
    fun (chatId, msgId) ->

        let inline createButtons (nodes: Graph.Node<ServiceInfo> seq) =
            nodes
            |> Seq.map (fun node -> (embassyId, node.FullId) |> Command.GetService |> Command.set, node.ShortName)
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

                    let command = (embassyId, serviceId, "{вставить сюда}") |> Command.SetService |> Command.set
                    let doubleLine = Environment.NewLine + Environment.NewLine
                    let message = $"%s{command}%s{doubleLine}"

                    node.Value.Instruction
                    |> Option.map (fun instruction -> message + $"Инструкция:%s{doubleLine}%s{instruction}")
                    |> Option.defaultValue message
                    |> Text.create (chatId, msgId |> Replace)
                | services -> services |> createButtons)
            |> async.Return
            
let internal setService (embassyId, serviceId, payload) =
    fun (chatId, msgId) ->

        Service.GRAPH
        |> Graph.BFS.tryFindById serviceId
        |> Option.map Ok
        |> Option.defaultValue ("Не могу найти выбранную услугу" |> NotFound |> Error)
        |> Result.map (fun node ->
            node.Children
            |> Seq.tryFind (fun node -> node.Value.Instruction.IsSome)
            |> Option.map (fun node -> node.Value.Instruction.Value)
            |> Option.defaultValue "Услуга успешно выбрана"
            |> Text.create (chatId, New))
        |> async.Return
