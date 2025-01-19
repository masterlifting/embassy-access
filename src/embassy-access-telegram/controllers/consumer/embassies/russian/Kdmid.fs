[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.Russian.Kdmid

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer.Embassies.Russian
open EA.Telegram.Endpoints.Consumer.Embassies.Russian.Kdmid
open EA.Telegram.Services.Consumer.Embassies.Russian.Kdmid

let get request =
    fun (deps: Russian.Dependencies) ->
        Kdmid.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get.Appointments requestId -> Query.getAppointments requestId
            |> fun createResponse -> deps |> createResponse |> deps.sendResult)

let post request =
    fun (deps: Russian.Dependencies) ->
        Kdmid.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Post.Subscribe model -> Command.subscribe model >> deps.sendResult
            | Post.CheckAppointments model -> Command.checkAppointments model >> deps.sendResult
            | Post.SendAppointments model -> Command.sendAppointments model >> deps.sendResults
            | Post.ConfirmAppointment model -> Command.confirmAppointment model >> deps.sendResult
            |> fun send -> deps |> send)
