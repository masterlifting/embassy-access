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

let private createBaseRoute method =
    Router.Services(Services.Method.Russian(Method.Midpass(method)))

let menu (requestId: RequestId) =
    fun (deps: Midpass.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map (fun r ->
            let print = Midpass.Method.Get(Midpass.Get.Print r.Id) |> createBaseRoute
            let delete =
                Midpass.Method.Delete(Midpass.Delete.Subscription(r.Id)) |> createBaseRoute

            ButtonsGroup.create {
                Name = "Manage your subscription"
                Columns = 1
                Buttons =
                    [ print.Value, "Print"; delete.Value, "Delete" ]
                    |> Seq.map (fun (callback, name) -> Button.create name (CallbackData callback))
                    |> Set.ofSeq
            }
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)

let print (requestId: RequestId) =
    fun (deps: Midpass.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map Request.print<Payload>
        |> ResultAsync.map (Text.create >> Message.createNew deps.ChatId)

[<Literal>]
let private INPUT_NUMBER = "<number>"

let private (|CheckStatus|ServiceNotFound|) (serviceId: ServiceId) =
    match serviceId.Value |> Graph.NodeId.splitValues with
    | [ _; _; _; "1" ] -> CheckStatus
    | _ -> ServiceNotFound

let private createMidpassInstruction chatId method =
    let route = Midpass.Method.Post(method) |> createBaseRoute

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

let private getUserSubscriptions (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Midpass.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequests embassyId serviceId)
        |> ResultAsync.map (fun requests ->
            requests
            |> Seq.map (fun r ->
                let route = Midpass.Method.Get(Midpass.Get.Menu r.Id) |> createBaseRoute
                route.Value, r.Payload |> Payload.print)
            |> fun buttons ->
                ButtonsGroup.create {
                    Name = "Your subscriptions to manage"
                    Columns = 1
                    Buttons =
                        buttons
                        |> Seq.map (fun (callback, name) -> Button.create name (CallbackData callback))
                        |> Set.ofSeq
                }
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)

let getService (serviceId: ServiceId) (embassyId: EmbassyId) forUser =
    fun (deps: Midpass.Dependencies) ->
        match forUser with
        | true -> deps |> getUserSubscriptions serviceId embassyId
        | false ->
            match serviceId with
            | CheckStatus -> deps |> checkStatus serviceId embassyId
            | ServiceNotFound ->
                $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
                |> async.Return
