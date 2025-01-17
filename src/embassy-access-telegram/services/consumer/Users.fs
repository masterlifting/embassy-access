module EA.Telegram.Services.Consumer.Users

open System
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Endpoints.Consumer
open EA.Telegram.Endpoints.Consumer.Users

let private createButtons chatId messageId buttonGroupName data =
    (chatId, messageId |> Option.map Replace |> Option.defaultValue New)
    |> Buttons.create
        { Name = buttonGroupName |> Option.defaultValue "Choose what do you want to look at"
          Columns = 1
          Data = data |> Map.ofSeq }

let private toUserEmbassyResponse chatId messageId buttonGroupName userId (embassies: EmbassyNode seq) =
    embassies
    |> Seq.map (fun embassy -> Router.Users(Get(UserEmbassy(userId, embassy.Id))).Value, embassy.ShortName)
    |> createButtons chatId messageId buttonGroupName

let private toUserEmbassyServicesResponse
    chatId
    messageId
    buttonGroupName
    userId
    embassyId
    (services: ServiceNode seq)
    =
    services
    |> Seq.map (fun service ->
        Router.Users(Get(UserEmbassyService(userId, embassyId, service.Id))).Value, service.ShortName)
    |> createButtons chatId messageId buttonGroupName

module internal Get =
    open EA.Telegram.Services.Consumer.Embassies.Embassies

    let userEmbassies userId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassyNodes userId
            |> ResultAsync.map (Seq.map _.Value)
            |> ResultAsync.map (toUserEmbassyResponse deps.ChatId None None userId)

    let userEmbassy userId embassyId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassy userId embassyId
            |> ResultAsync.bindAsync (fun (embassy, embassyСhildren) ->
                match embassyСhildren with
                | [] -> deps.EmbassiesDeps |> Get.embassyServices embassy.Id
                | _ ->
                    embassyСhildren
                    |> toUserEmbassyResponse deps.ChatId (Some deps.MessageId) embassy.Description userId
                    |> Ok
                    |> async.Return)

    let userEmbassyServices userId embassyId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassyServiceNodes userId embassyId
            |> ResultAsync.map (Seq.map _.Value)
            |> ResultAsync.map (toUserEmbassyServicesResponse deps.ChatId None None userId embassyId)

    let userEmbassyService userId embassyId serviceId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassyServiceNode userId embassyId serviceId
            |> ResultAsync.bindAsync (function
                | AP.Leaf _ -> deps.EmbassiesDeps |> Get.embassyService embassyId serviceId
                | AP.Node node ->
                    node.Children
                    |> Seq.map _.Value
                    |> toUserEmbassyServicesResponse
                        deps.ChatId
                        (Some deps.MessageId)
                        node.Value.Description
                        userId
                        embassyId
                    |> Ok
                    |> async.Return)
