[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Consumer.Users

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Routes.Users
open System
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram.Routes

let private createButtons chatId msgIdOpt name data =
    (chatId, msgIdOpt |> Option.map Replace |> Option.defaultValue New)
    |> Buttons.create
        { Name = name |> Option.defaultValue "Choose what do you want to visit"
          Columns = 3
          Data = data |> Map.ofSeq }

module internal Get =
    let userEmbassies userId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassies userId
            |> ResultAsync.map (fun embassies ->
                embassies
                |> Seq.map (fun node ->
                    let request = Router.Users(Get(UserEmbassy(userId, node.FullId)))
                    request.Route, node.ShortName)
                |> createButtons deps.ChatId (Some deps.MessageId) None)

    let userEmbassy userId embassyId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassy userId embassyId
            |> ResultAsync.bindAsync (fun embassyNode ->
                match embassyNode.Children with
                | [] ->
                    deps.EmbassiesDeps
                    |> EA.Telegram.Handlers.Consumer.Embassies.Get.embassyServices embassyNode.FullId
                | children ->
                    children
                    |> Seq.map (fun node ->
                        let request = Router.Users(Get(UserEmbassy(userId, node.FullId)))
                        request.Route, node.ShortName)
                    |> createButtons deps.ChatId (Some deps.MessageId) embassyNode.Value.Description
                    |> Ok
                    |> async.Return)

let consume request =
    fun (deps: Core.Dependencies) ->
        Users.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | UserEmbassies userId -> deps |> Get.userEmbassies userId
                | UserEmbassy(userId, embassyId) -> deps |> Get.userEmbassy userId embassyId)
