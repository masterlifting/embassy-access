[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.Russian.Kdmid

open Infrastructure.Prelude
open EA.Telegram.Endpoints.Embassies.Russian.Kdmid
open EA.Telegram.Dependencies.Consumer.Embassies.Russian
open EA.Telegram.Services.Consumer.Embassies.Russian.Kdmid

let get request =
    fun (dependencies: Russian.Dependencies) ->
        Kdmid.Dependencies.create dependencies
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get.Appointments requestId -> Query.getAppointments requestId
            | Get.SubscriptionsMenu requestId -> Query.getSubscriptionsMenu requestId
            |> fun createResponse -> deps |> (createResponse >> dependencies.translate) |> deps.sendResult)

let post request =
    fun (dependencies: Russian.Dependencies) ->
        Kdmid.Dependencies.create dependencies
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Post.Subscribe model -> Command.subscribe model >> dependencies.translate >> deps.sendResult
            | Post.CheckAppointments model ->
                Command.checkAppointments model >> dependencies.translate >> deps.sendResult
            | Post.SendAppointments model ->
                Command.sendAppointments model >> dependencies.translateSeq >> deps.sendResults
            | Post.ConfirmAppointment model ->
                Command.confirmAppointment model >> dependencies.translate >> deps.sendResult
            |> fun send -> deps |> send)

let delete request =
    fun (dependencies: Russian.Dependencies) ->
        Kdmid.Dependencies.create dependencies
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Delete.Subscription requestId -> Command.deleteSubscription requestId
            |> fun createResponse -> deps |> (createResponse >> dependencies.translate) |> deps.sendResult)
