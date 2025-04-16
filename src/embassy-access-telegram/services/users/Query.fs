module EA.Telegram.Services.Users.Query

open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Users
open EA.Telegram.Dependencies
open EA.Telegram.Services

let private createMessage chatId msgIdOpt nameOpt data =
    let name = nameOpt |> Option.defaultValue "Choose from the list"

    match data |> Seq.length with
    | 0 -> Text.create $"No data for the '{name}'"
    | _ ->
        ButtonsGroup.create {
            Name = name
            Columns = 1
            Buttons =
                data
                |> Seq.map (fun (callback, name) -> callback |> CallbackData |> Button.create name)
                |> Set.ofSeq
        }
    |> Message.tryReplace msgIdOpt chatId

let private toUserEmbassiesResponse chatId messageId buttonGroupName (embassies: Embassy seq) =
    embassies
    |> Seq.map (fun embassy -> Router.Users(Method.Get(Get.UserEmbassy embassy.Id)).Value, embassy.Name)
    |> createMessage chatId messageId buttonGroupName

let private toUserEmbassyServicesResponse chatId messageId buttonGroupName embassyId (services: Service seq) =
    services
    |> Seq.map (fun service ->
        Router.Users(Method.Get(Get.UserEmbassyService(embassyId, service.Id))).Value, service.Name)
    |> createMessage chatId messageId buttonGroupName

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
            | [] -> deps.Embassies |> Embassies.Query.getUserEmbassyService embassyId serviceId
            | _ ->
                services
                |> toUserEmbassyServicesResponse deps.Chat.Id (Some deps.MessageId) parentDescription embassyId
                |> Ok
                |> async.Return)
