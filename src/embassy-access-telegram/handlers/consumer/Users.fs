[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Consumer.Users

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Routes.Users
open System
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram.Routes
open EA.Telegram.Routes.Embassies

let private createButtons chatId msgIdOpt name data =
    (chatId, msgIdOpt |> Option.map Replace |> Option.defaultValue New)
    |> Buttons.create
        { Name = name |> Option.defaultValue "Choose what do you want to visit"
          Columns = 3
          Data = data |> Map.ofSeq }

let getEmbassies userId =
    fun (deps: Users.Dependencies) ->
        deps.getEmbassies userId
        |> ResultAsync.bindAsync (fun embassies ->
            embassies
            |> Seq.map (fun node ->
                let request = node.FullId |> GetRequest.Id |> Get |> Router.Request.Embassies
                request.Route, node.ShortName)
            |> createButtons deps.ChatId (Some deps.MessageId) None
            |> Ok
            |> async.Return)

let consume request =
    fun (deps: Core.Dependencies) ->
        Users.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Request.Get getRequest ->
                match getRequest with
                | GetRequest.Id id -> "" |> NotSupported |> Error |> async.Return
                | GetRequest.All -> "" |> NotSupported |> Error |> async.Return
                | GetRequest.Embassies userId -> deps |> getEmbassies userId)
