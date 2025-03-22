[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.Russian.Kdmid

open Infrastructure.Prelude
open EA.Telegram.Endpoints.Embassies.Russian.Kdmid
open EA.Telegram.Dependencies.Consumer.Embassies.Russian
open EA.Telegram.Services
open EA.Telegram.Services.Consumer.Embassies.Russian.Kdmid

let get request =
    fun (deps: Russian.Dependencies) ->
        Kdmid.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->

            let translate msgRes =
                deps.Culture.Base
                |> Producer.Culture.Command.translateRes deps.Chat.Culture msgRes deps.Culture.Placeholder

            let sendResult getResponse =
                deps |> (getResponse >> translate) |> deps.sendResult

            match request with
            | Get.Appointments requestId -> Query.getAppointments requestId
            | Get.SubscriptionsMenu requestId -> Query.getSubscriptionsMenu requestId
            |> sendResult)

let post request =
    fun (deps: Russian.Dependencies) ->
        Kdmid.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->

            let translate msgRes =
                deps.Culture.Base
                |> Producer.Culture.Command.translateRes deps.Chat.Culture msgRes deps.Culture.Placeholder

            let translateSeq msgSeqRes =
                deps.Culture.Base
                |> Producer.Culture.Command.translateSeqRes deps.Chat.Culture msgSeqRes deps.Culture.Placeholder

            match request with
            | Post.Subscribe model -> Command.subscribe model >> translate >> deps.sendResult
            | Post.CheckAppointments model -> Command.checkAppointments model >> translate >> deps.sendResult
            | Post.SendAppointments model -> Command.sendAppointments model >> translateSeq >> deps.sendResults
            | Post.ConfirmAppointment model -> Command.confirmAppointment model >> translate >> deps.sendResult
            |> fun send -> deps |> send)

let delete request =
    fun (deps: Russian.Dependencies) ->
        Kdmid.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->

            let translate msgRes =
                deps.Culture.Base
                |> Producer.Culture.Command.translateRes deps.Chat.Culture msgRes deps.Culture.Placeholder

            let sendResult getResponse =
                deps |> (getResponse >> translate) |> deps.sendResult

            match request with
            | Delete.Subscription requestId -> Command.deleteSubscription requestId
            |> sendResult)
