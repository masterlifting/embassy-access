module EA.Telegram.Services.Consumer.Users.Service

open System
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Endpoints.Consumer
open EA.Telegram.Endpoints.Consumer.Users
open EA.Telegram.Endpoints.Consumer.Users.Request

let private createButtons chatId msgIdOpt buttonGroupName data =
    match data |> Seq.length with
    | 0 -> Text.create "No data"
    | _ ->
        Buttons.create
            { Name = buttonGroupName |> Option.defaultValue "Choose what do you want to visit"
              Columns = 1
              Data = data |> Map.ofSeq }
    |> fun send -> (chatId, msgIdOpt |> Option.map Replace |> Option.defaultValue New) |> send

let private toUserEmbassyResponse chatId messageId buttonGroupName (embassies: EmbassyNode seq) =
    embassies
    |> Seq.map (fun embassy -> Request.Users(Get(Get.UserEmbassy(embassy.Id))).Value, embassy.ShortName)
    |> createButtons chatId messageId buttonGroupName

let private toUserEmbassyServicesResponse chatId messageId buttonGroupName embassyId (services: ServiceNode seq) =
    services
    |> Seq.map (fun service ->
        Request.Users(Get(Get.UserEmbassyService(embassyId, service.Id))).Value, service.ShortName)
    |> createButtons chatId messageId buttonGroupName

module internal Query =
    open EA.Telegram.Services.Consumer.Embassies.Service

    let getUserEmbassies () =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassies ()
            |> ResultAsync.map (fun (parentDescription, embassies) ->
                embassies |> toUserEmbassyResponse deps.ChatId None parentDescription)

    let getUserEmbassyServices embassyId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassyServices embassyId
            |> ResultAsync.map (fun (parentDescription, services) ->
                services
                |> toUserEmbassyServicesResponse deps.ChatId (Some deps.MessageId) parentDescription embassyId)

    let getUserEmbassy embassyId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassyChildren embassyId
            |> ResultAsync.bindAsync (fun (parentDescription, embassies) ->
                match embassies with
                | [] -> deps |> getUserEmbassyServices embassyId
                | _ ->
                    embassies
                    |> toUserEmbassyResponse deps.ChatId (Some deps.MessageId) parentDescription
                    |> Ok
                    |> async.Return)

    let getUserEmbassyService embassyId serviceId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassyServiceChildren embassyId serviceId
            |> ResultAsync.bindAsync (fun (parentDescription, services) ->
                match services with
                | [] -> deps.EmbassiesDeps |> Get.userEmbassyService embassyId serviceId
                | _ ->
                    services
                    |> toUserEmbassyServicesResponse deps.ChatId (Some deps.MessageId) parentDescription embassyId
                    |> Ok
                    |> async.Return)
