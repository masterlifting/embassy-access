module EA.Telegram.CommandHandler.Russian.Core

open System
open EA.Core.Domain
open EA.Embassies.Russian.Kdmid.Domain
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram

let internal getService (embassyId, serviceIdOpt) =
    fun (deps: Dependencies.GetService) ->

        let inline createButtons buttonName (nodes: Graph.Node<ServiceGraph> seq) =
            nodes
            |> Seq.map (fun node -> (embassyId, node.FullId) |> Command.GetService |> Command.set, node.ShortName)
            |> Map
            |> fun buttons ->
                { Buttons.Name = buttonName |> Option.defaultValue "Какую услугу вы хотите получить?"
                  Columns = 1
                  Data = buttons }
                |> Buttons.create (deps.ChatId, deps.MessageId |> Replace)

        deps.getServiceGraph ()
        |> ResultAsync.bind (fun graph ->
            match serviceIdOpt with
            | None -> graph.Children |> createButtons graph.Value.Description |> Ok
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
                        |> Text.create (deps.ChatId, deps.MessageId |> Replace)
                    | services -> services |> createButtons node.Value.Description))

let internal setService (serviceId, embassy, payload) =
    fun (deps: Dependencies.SetService) ->

        let inline createOrUpdateChat (request: Request) =
            deps.createOrUpdateChat
                { Id = deps.ChatId
                  Subscriptions = [ request.Id ] |> Set }

        let inline createOrUpdateRequest serviceName =
            let serviceRequest: KdmidRequest =
                { Uri = Uri(payload)
                  Embassy = embassy
                  TimeZone = 1.0
                  Confirmation = Disabled }

            serviceName |> serviceRequest.CreateRequest |> deps.createOrUpdateRequest

        deps.getServiceGraph ()
        |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
        |> ResultAsync.bind (function
            | Some node -> Ok node
            | None -> $"ServiceId {serviceId.Value}" |> NotFound |> Error)
        |> ResultAsync.bindAsync (fun node ->
            match node.Children with
            | [] ->
                match node.FullId with
                | Graph.NodeIdValue "EMB.PASS.CHK" -> node.ShortName |> NotSupported |> Error |> async.Return
                | _ ->
                    createOrUpdateRequest node.FullName
                    |> ResultAsync.bindAsync createOrUpdateChat
                    |> ResultAsync.map (fun _ -> $"Заявка на услугу '{node.FullName}' успешно создана")
                    |> ResultAsync.map (fun message -> message |> Text.create (deps.ChatId, New))
            | _ -> node.FullName |> NotSupported |> Error |> async.Return)
