[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Embassies.Russian.Kdmid

open Infrastructure.Prelude
open EA.Telegram.Endpoints.Embassies.Russian.Kdmid
open EA.Telegram.Dependencies.Embassies.Russian
open EA.Telegram.Services.Culture
open EA.Telegram.Services.Embassies.Russian.Kdmid

let get request =
    fun (deps: Russian.Dependencies) ->
        Kdmid.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->

            let translate msgRes =
                deps.Culture
                |> Message.translateRes deps.Chat.Culture msgRes

            let sendResult getResponse =
                deps |> (getResponse >> translate) |> deps.sendMessageRes

            match request with
            | Get.Appointments requestId -> Query.getAppointments requestId
            | Get.SubscriptionsMenu requestId -> Query.getSubscriptionsMenu requestId
            |> sendResult)

let post request =
    fun (deps: Russian.Dependencies) ->
        Kdmid.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->

            let translate msgRes =
                deps.Culture
                |> Message.translateRes deps.Chat.Culture msgRes

            let translateSeq msgSeqRes =
                deps.Culture
                |> Message.translateSeqRes deps.Chat.Culture msgSeqRes

            match request with
            | Post.Subscribe model -> Command.subscribe model >> translate >> deps.sendMessageRes
            | Post.CheckAppointments model -> Command.checkAppointments model >> translate >> deps.sendMessageRes
            | Post.SendAppointments model -> Command.sendAppointments model >> translateSeq >> deps.sendMessagesRes
            | Post.ConfirmAppointment model -> Command.confirmAppointment model >> translate >> deps.sendMessageRes
            |> fun send -> deps |> send)

let delete request =
    fun (deps: Russian.Dependencies) ->
        Kdmid.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->

            let translate msgRes =
                deps.Culture
                |> Message.translateRes deps.Chat.Culture msgRes

            let sendResult getResponse =
                deps |> (getResponse >> translate) |> deps.sendMessageRes

            match request with
            | Delete.Subscription requestId -> Command.deleteSubscription requestId
            |> sendResult)
