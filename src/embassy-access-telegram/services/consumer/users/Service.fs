module EA.Telegram.Services.Consumer.Users.Service

open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Endpoints
open EA.Telegram.Endpoints.Users
open EA.Telegram.Endpoints.Users.Request
open EA.Telegram.Dependencies.Consumer

let private createMessage chatId msgIdOpt nameOpt data =
    match data |> Seq.length with
    | 0 -> Text.create "No data"
    | _ ->
        ButtonsGroup.create
            { Name = nameOpt |> Option.defaultValue "Choose what do you want"
              Columns = 1
              Buttons =
                data
                |> Seq.map (fun (callback, name) -> callback |> CallbackData |> Button.create name)
                |> Set.ofSeq }
    |> Message.tryReplace msgIdOpt chatId

let private toUserEmbassiesResponse chatId messageId buttonGroupName (embassies: EmbassyNode seq) =
    embassies
    |> Seq.map (fun embassy -> Request.Users(Get(Get.UserEmbassy(embassy.Id))).Value, embassy.ShortName)
    |> createMessage chatId messageId buttonGroupName

let private toUserEmbassyServicesResponse chatId messageId buttonGroupName embassyId (services: ServiceNode seq) =
    services
    |> Seq.map (fun service ->
        Request.Users(Get(Get.UserEmbassyService(embassyId, service.Id))).Value, service.ShortName)
    |> createMessage chatId messageId buttonGroupName

module internal Query =
    open EA.Telegram.Services.Consumer

    let getUserEmbassies () =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassies ()
            |> ResultAsync.map (fun (parentDescription, embassies) ->
                embassies |> toUserEmbassiesResponse deps.Chat.Id None parentDescription)

    let getUserEmbassyServices embassyId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassyServices embassyId
            |> ResultAsync.map (fun (parentDescription, services) ->
                services
                |> toUserEmbassyServicesResponse deps.Chat.Id (Some deps.MessageId) parentDescription embassyId)

    let getUserEmbassy embassyId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassyChildren embassyId
            |> ResultAsync.bindAsync (fun (parentDescription, embassies) ->
                match embassies with
                | [] -> deps |> getUserEmbassyServices embassyId
                | _ ->
                    embassies
                    |> toUserEmbassiesResponse deps.Chat.Id (Some deps.MessageId) parentDescription
                    |> Ok
                    |> async.Return)

    let getUserEmbassyService embassyId serviceId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassyServiceChildren embassyId serviceId
            |> ResultAsync.bindAsync (fun (parentDescription, services) ->
                match services with
                | [] ->
                    deps.EmbassiesDeps
                    |> Embassies.Service.Query.getUserEmbassyService embassyId serviceId
                | _ ->
                    services
                    |> toUserEmbassyServicesResponse deps.Chat.Id (Some deps.MessageId) parentDescription embassyId
                    |> Ok
                    |> async.Return)
