module EA.Telegram.Services.Services.Russian.Midpass.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Services.Russian
open EA.Telegram.Dependencies.Services.Russian
open EA.Russian.Services.Domain.Midpass

let print (requestId: RequestId) =
    fun (deps: Midpass.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map Request.print<Payload>
        |> ResultAsync.map (Text.create >> Message.tryReplace (Some deps.MessageId) deps.ChatId)

[<Literal>]
let private INPUT_NUMBER = "<number>"

let private (|CheckStatus|ServiceNotFound|) (serviceId: ServiceId) =
    match serviceId.Value |> Graph.NodeId.splitValues with
    | [ _; _; _; "1" ] -> CheckStatus
    | _ -> ServiceNotFound

let private createMidpassInstruction chatId method =
    let route =
        Router.Services(Services.Method.Russian(Method.Midpass(Midpass.Method.Post(method))))
    $"To use this service, please send the following command back to the bot: {String.addLines 2}'{route.Value}'"
    + $"{String.addLines 2}Replace the {INPUT_NUMBER} with the number that you received in the Russian embassy."
    + $"{String.addLines 1}Send the command without apostrophes, please."
    |> Text.create
    |> Message.createNew chatId
    |> Ok
    |> async.Return

let private checkStatus (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Midpass.Dependencies) ->
        Midpass.Post.CheckStatus(serviceId, embassyId, INPUT_NUMBER)
        |> createMidpassInstruction deps.ChatId

let getService (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Midpass.Dependencies) ->
        match serviceId with
        | CheckStatus -> deps |> checkStatus serviceId embassyId
        | ServiceNotFound ->
            $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return
