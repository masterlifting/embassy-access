module EA.Telegram.CommandHandler.Russian

open System
open Infrastructure
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram
open EA.Embassies.Russian.Domain

let service (country, serviceNameOpt) =
    fun (chatId, msgId) ->

        let inline createButtons services =
            services
            |> Seq.map (fun service ->
                let buttonName = service.Name |> Graph.splitNodeName |> Seq.last
                (country, service.Name) |> Command.RussianService |> Command.set, buttonName)
            |> Map
            |> fun data ->
                { Buttons.Name = "Какую услугу вы хотите получить?"
                  Columns = 1
                  Data = data }
                |> Buttons.create (chatId, msgId |> Replace)

        match serviceNameOpt with
        | None ->
            Service.GRAPH.Children None
            |> Seq.map _.Value
            |> createButtons
            |> Ok
            |> async.Return
        | Some serviceName ->
            Service.GRAPH
            |> Graph.findNode serviceName
            |> Option.map Ok
            |> Option.defaultValue (serviceName |> NotFound |> Error)
            |> Result.map (fun node ->
                let services = serviceName |> Some |> node.Children |> Seq.map _.Value |> Seq.toList

                match services with
                | [] ->

                    let command =
                        (EA.Core.Domain.Russian country, serviceName)
                        |> Command.ServiceGet
                        |> Command.set

                    let message =
                        $"%s{command}{Environment.NewLine}Отправьте назад вышеуказанную комманду для получения услуги."

                    node.Value.Description
                    |> Option.map (fun instruction -> message + $"{Environment.NewLine}Инструкция: %s{instruction}")
                    |> Option.defaultValue message
                    |> Text.create (chatId, msgId |> Replace)
                | _ -> services |> createButtons)
            |> async.Return
