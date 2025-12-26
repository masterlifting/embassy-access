module EA.Telegram.Features.Embassies.Russian.Midpass.Query

open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Russian.Services.Domain.Midpass
open EA.Telegram.Router.Embassies
open EA.Telegram.Router.Embassies.Russian
open EA.Telegram.Router.Embassies.Russian.Midpass
open EA.Telegram.Features.Dependencies.Embassies.Russian

let private buildRoute route =
    EA.Telegram.Router.Route.Embassies(Russian(Midpass route))

let menu (requestId: RequestId) =
    fun (deps: Midpass.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map (fun r ->
            let info = Get(Info r.Id) |> buildRoute
            let delete = Delete(Subscription r.Id) |> buildRoute

            ButtonsGroup.create {
                Name = r.Service.Value.BuildName 1 "."
                Columns = 1
                Buttons = [ "Info", info.Value; "Delete", delete.Value ] |> ButtonsGroup.createButtons
            }
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)

let info (requestId: RequestId) =
    fun (deps: Midpass.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map Request.print<Payload>
        |> ResultAsync.map (Text.create >> Message.createNew deps.ChatId)

[<Literal>]
let private INPUT_NUMBER = "<number>"

let private checkStatus (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Midpass.Dependencies) ->
        let route = Post(CheckStatus(serviceId, embassyId, INPUT_NUMBER)) |> buildRoute

        $"To use this service, please send the following command back to the bot:{String.addLines 2}'{route.Value}'"
        + $"{String.addLines 2}Replace {INPUT_NUMBER} with the number you received from the Russian embassy."
        + $"{String.addLines 1}Send the command without apostrophes, please."
        |> Text.create
        |> Message.createNew deps.ChatId
        |> Ok
        |> async.Return

let private getUserSubscriptions (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Midpass.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequests embassyId serviceId)
        |> ResultAsync.map (fun requests ->
            requests
            |> Seq.map (fun r ->
                let route = Get(Menu r.Id) |> buildRoute
                r.Service.Value.BuildName 1 ".", route.Value)
            |> fun buttons ->
                ButtonsGroup.create {
                    Name = "Your subscriptions"
                    Columns = 1
                    Buttons = buttons |> ButtonsGroup.createButtons
                }
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)

let getService (serviceId: ServiceId) (embassyId: EmbassyId) forUser =
    fun (deps: Midpass.Dependencies) ->
        match forUser with
        | true -> deps |> getUserSubscriptions serviceId embassyId
        | false -> deps |> checkStatus serviceId embassyId
