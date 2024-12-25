[<RequireQualifiedAccess>]
module EA.Telegram.Consumer.Handlers.Users

open System
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Consumer.Dependencies
open EA.Telegram.Consumer.Endpoints
open EA.Telegram.Consumer.Endpoints.Users

let private createButtons chatId messageId buttonGroupName data =
    (chatId, messageId |> Option.map Replace |> Option.defaultValue New)
    |> Buttons.create
        { Name = buttonGroupName |> Option.defaultValue "Choose what do you want to look at"
          Columns = 1
          Data = data |> Map.ofSeq }

let private toUserEmbassyResponse chatId messageId buttonGroupName userId (embassies: EmbassyNode seq) =
    embassies
    |> Seq.map (fun embassy -> Core.Users(Get(UserEmbassy(userId, embassy.Id))).Route, embassy.Name)
    |> createButtons chatId messageId buttonGroupName
    
let private toUserEmbassyServicesResponse chatId messageId buttonGroupName  userId embassyId (services: ServiceNode seq) =
    services
    |> Seq.map (fun service -> Core.Users(Get(UserEmbassyServices(userId, service.Id))).Route, service.Name)
    |> createButtons chatId messageId buttonGroupName

module internal Get =
    let userEmbassies userId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassies userId
            |> ResultAsync.map (toUserEmbassyResponse deps.ChatId None userId None)

    let userEmbassy userId embassyId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassyNode userId embassyId
            |> ResultAsync.bindAsync (function
                | AP.Leaf value ->
                    deps.EmbassiesDeps
                    |> EA.Telegram.Consumer.Handlers.Embassies.Get.embassyServices value.Id
                | AP.Node node ->
                    node.Children
                    |> Seq.map _.Value
                    |> toUserEmbassyResponse deps.ChatId (Some deps.MessageId) userId node.Value.Description
                    |> Ok
                    |> async.Return)
    
    let getUserEmbassyServices userId embassyId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassyServices userId embassyId
            

let toResponse request =
    fun (deps: Core.Dependencies) ->
        Users.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | UserEmbassies userId -> deps |> Get.userEmbassies userId
                | UserEmbassy(userId, embassyId) -> deps |> Get.userEmbassy userId embassyId)