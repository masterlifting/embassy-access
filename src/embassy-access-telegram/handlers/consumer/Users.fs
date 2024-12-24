[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Consumer.Users

open System
open EA.Core.Domain
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Routes.Users
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer

let private createButtons chatId msgIdOpt name data =
    (chatId, msgIdOpt |> Option.map Replace |> Option.defaultValue New)
    |> Buttons.create
        { Name = name |> Option.defaultValue "Choose what do you want to visit"
          Columns = 3
          Data = data |> Map.ofSeq }

let private toUserEmbassyResponse chatId messageId userId name (embassyNodes: Graph.Node<EmbassyNode> seq) =
    embassyNodes
    |> Seq.map (fun embassyNode ->
        EA.Telegram.Routes.Router
            .Users(Get(UserEmbassy(userId, embassyNode.FullId)))
            .Route,
        embassyNode.ShortName)
    |> createButtons chatId (Some messageId) name

module internal Get =
    let userEmbassies userId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassies userId
            |> ResultAsync.map (toUserEmbassyResponse deps.ChatId deps.MessageId userId None)

    let userEmbassy userId embassyId =
        fun (deps: Users.Dependencies) ->
            deps.getUserEmbassy userId embassyId
            |> ResultAsync.bindAsync (fun embassyNode ->
                match embassyNode.Children with
                | [] ->
                    deps.EmbassiesDeps
                    |> EA.Telegram.Handlers.Consumer.Embassies.Get.embassyNodeServices embassyNode
                | children ->
                    children
                    |> toUserEmbassyResponse deps.ChatId deps.MessageId userId embassyNode.Value.Description
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
