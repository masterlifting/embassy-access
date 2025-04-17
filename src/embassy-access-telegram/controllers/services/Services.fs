﻿[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Services

let respond request chat =
    fun (deps: Request.Dependencies) ->
        Embassies.Dependencies.create chat deps
        |> ResultAsync.wrap (fun deps ->
            let inline processSingleMessage handler =
                handler >> deps.translateMessageRes >> deps.sendMessageRes

            let inline processMultipleMessages handler =
                handler >> deps.translateMessagesRes >> deps.sendMessagesRes

            match request with

            | Method.Get get ->
                let handler =
                    match get with
                    | Get.Embassy embassyId -> Query.getEmbassy embassyId
                    | Get.Embassies -> Query.getEmbassies ()
                    | Get.UserEmbassy embassyId -> Query.getUserEmbassy embassyId
                    | Get.UserEmbassies -> Query.getUserEmbassies ()
                processSingleMessage handler <| deps

            | Method.Post post ->
                let handler =
                    match post with
                    | Post.Subscribe model -> model |> Command.subscribe |> processSingleMessage
                    | Post.CheckAppointments model -> model |> Command.checkAppointments |> processSingleMessage
                    | Post.SendAppointments model -> model |> Command.sendAppointments |> processMultipleMessages
                    | Post.ConfirmAppointment model -> model |> Command.confirmAppointment |> processSingleMessage
                handler deps

            | Method.Delete delete ->
                match delete with
                | Delete.Subscription requestId -> processSingleMessage (Command.deleteSubscription requestId) <| deps)
