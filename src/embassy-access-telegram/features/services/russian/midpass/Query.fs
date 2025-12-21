module EA.Telegram.Features.Services.Russian.Midpass.Query

open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Russian.Services.Domain.Midpass
open EA.Telegram.Features.Router.Services.Root
open EA.Telegram.Features.Router.Services.Russian.Root
open EA.Telegram.Features.Router.Services.Russian.Midpass
open EA.Telegram.Features.Dependencies.Services.Russian

let menu (requestId: RequestId) =
    fun (deps: Midpass.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map (fun r ->
            let info = Russian(Midpass(Get(Info r.Id)))
            let delete = Russian(Midpass(Delete(Subscription r.Id)))

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

let private createMidpassInstruction chatId request =
    let route = Russian(Midpass(Post request))

    $"To use this service, please send the following command back to the bot:{String.addLines 2}'{route.Value}'"
    + $"{String.addLines 2}Replace {INPUT_NUMBER} with the number you received from the Russian embassy."
    + $"{String.addLines 1}Send the command without apostrophes, please."
    |> Text.create
    |> Message.createNew chatId
    |> Ok
    |> async.Return

let private checkStatus (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Midpass.Dependencies) ->
        CheckStatus(serviceId, embassyId, INPUT_NUMBER)
        |> createMidpassInstruction deps.ChatId

let private getUserSubscriptions (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Midpass.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequests embassyId serviceId)
        |> ResultAsync.map (fun requests ->
            requests
            |> Seq.map (fun r ->
                let route = Russian(Midpass(Get(Menu r.Id)))
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
