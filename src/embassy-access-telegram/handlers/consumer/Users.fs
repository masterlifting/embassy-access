[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Consumer.Users

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
    |> Seq.map (fun embassy -> Core.Users(Get(UserEmbassy(userId, embassy.Id))).Route, embassy.ShortName)
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
        Core.Users(Get(UserEmbassyService(userId, embassyId, service.Id))).Route, service.ShortName)
    |> createButtons chatId messageId buttonGroupName

module internal Get =
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
                | [] ->
                    deps.EmbassiesDeps
                    |> EA.Telegram.Handlers.Consumer.Embassies.Core.Get.embassyServices embassy.Id
                | _ ->
                    embassyСhildren
                    |> toUserEmbassyResponse deps.ChatId (Some deps.MessageId) embassy.Description userId
                    |> Ok
                    |> async.Return)

    let getUserEmbassyServices userId embassyId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassyServiceNodes userId embassyId
            |> ResultAsync.map (Seq.map _.Value)
            |> ResultAsync.map (toUserEmbassyServicesResponse deps.ChatId None None userId embassyId)

    let getUserEmbassyService userId embassyId serviceId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassyServiceNode userId embassyId serviceId
            |> ResultAsync.bindAsync (function
                | AP.Leaf _ ->
                    deps.EmbassiesDeps
                    |> EA.Telegram.Handlers.Consumer.Embassies.Core.Get.embassyService embassyId serviceId
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

let toResponse request =
    fun (deps: Core.Dependencies) ->
        Users.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | UserEmbassies userId -> deps |> Get.userEmbassies userId
                | UserEmbassy(userId, embassyId) -> deps |> Get.userEmbassy userId embassyId
                | UserEmbassyServices(userId, embassyId) -> deps |> Get.getUserEmbassyServices userId embassyId
                | UserEmbassyService(userId, embassyId, serviceId) ->
                    deps |> Get.getUserEmbassyService userId embassyId serviceId)
