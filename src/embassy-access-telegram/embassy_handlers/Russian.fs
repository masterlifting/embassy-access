module EA.Telegram.CommandHandler.Russian

open System
open EA.Core.Domain
open Infrastructure
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram
open EA.Embassies.Russian

let internal getService (embassyId, serviceIdOpt) =
    fun (cfg, chatId, msgId) ->

        let inline createButtons buttonName (nodes: Graph.Node<Domain.ServiceInfo> seq) =
            nodes
            |> Seq.map (fun node -> (embassyId, node.FullId) |> Command.GetService |> Command.set, node.ShortName)
            |> Map
            |> fun buttons ->
                { Buttons.Name = buttonName |> Option.defaultValue "Какую услугу вы хотите получить?"
                  Columns = 1
                  Data = buttons }
                |> Buttons.create (chatId, msgId |> Replace)

        cfg
        |> Settings.ServiceInfo.getGraph
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
                        |> Text.create (chatId, msgId |> Replace)
                    | services -> services |> createButtons node.Value.Description))

let internal setService (serviceId, embassy, payload) =
    fun (cfg, chatId, msgId, ct) ->

        let inline createOrUpdateChat request storage =

            let chat: EA.Telegram.Domain.Chat =
                { Id = chatId
                  Subscriptions = [request.Id] |> set }

            chat
            |> EA.Telegram.Persistence.Command.Chat.CreateOrUpdate
            |> EA.Telegram.Persistence.Repository.Command.Chat.execute storage ct

        let inline createOrUpdateRequest serviceName storage =
            let serviceRequest: Kdmid.Domain.ServiceRequest =
                { Uri = Uri(payload)
                  Embassy = embassy
                  TimeZone = 1.0
                  Confirmation = Disabled }

            serviceName
            |> serviceRequest.CreateRequest
            |> EA.Core.Persistence.Command.Request.CreateOrUpdate
            |> EA.Core.Persistence.Repository.Command.Request.execute storage ct

        let inline executeCreateOrUpdateChat cfg request =
            cfg
            |> EA.Telegram.Persistence.Storage.FileSystem.Chat.init
            |> ResultAsync.wrap (createOrUpdateChat request)

        let inline executeCreateOrUpdateRequest cfg name =
            cfg
            |> EA.Core.Persistence.Storage.FileSystem.Request.init
            |> ResultAsync.wrap (createOrUpdateRequest name)

        cfg
        |> Settings.ServiceInfo.getGraph
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
                    cfg
                    |> executeCreateOrUpdateChat
                    |> ResultAsync.bindAsync (fun _ -> executeCreateOrUpdateRequest cfg node.FullName)
                    |> ResultAsync.map (fun request -> $"Заявка на услугу '{node.FullName}' успешно создана")
                    |> ResultAsync.map (fun message -> message |> Text.create (chatId, New))
            | _ -> node.FullName |> NotSupported |> Error |> async.Return)
