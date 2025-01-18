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

    let getUserEmbassies userId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassies userId
            |> ResultAsync.map (fun (parentDescription, embassies) ->
                embassies |> toUserEmbassyResponse deps.ChatId None parentDescription userId)

    let getUserEmbassyServices userId embassyId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassyServices userId embassyId
            |> ResultAsync.map (fun (parentDescription, services) ->
                services
                |> toUserEmbassyServicesResponse deps.ChatId (Some deps.MessageId) parentDescription userId embassyId)

    let processUserEmbassy userId embassyId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassyChildren userId embassyId
            |> ResultAsync.bindAsync (fun (parentDescription, embassies) ->
                match embassies with
                | [] -> deps |> getUserEmbassyServices userId embassyId
                | _ ->
                    embassies
                    |> toUserEmbassyResponse deps.ChatId (Some deps.MessageId) parentDescription userId
                    |> Ok
                    |> async.Return)

    let processUserEmbassyService userId embassyId serviceId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassyServiceChildren userId embassyId serviceId
            |> ResultAsync.bindAsync (fun (parentDescription, services) ->
                match services with
                | [] -> deps.EmbassiesDeps |> Get.userEmbassyService userId embassyId serviceId
                | _ ->
                    services
                    |> toUserEmbassyServicesResponse
                        deps.ChatId
                        (Some deps.MessageId)
                        parentDescription
                        userId
                        embassyId
                    |> Ok
                    |> async.Return)
