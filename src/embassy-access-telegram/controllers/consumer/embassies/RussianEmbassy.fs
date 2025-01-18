[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.RussianEmbassy

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Dependencies.Consumer.Embassies
open EA.Telegram.Endpoints.Consumer.Embassies.Russian
open EA.Telegram.Endpoints.Consumer.Embassies.Russian.Request
open EA.Telegram.Services.Consumer.Embassies.Russian.Service

let respond request =
    fun (deps: Consumer.Dependencies) ->
        RussianEmbassy.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Get.Request.KdmidCheckAppointments requestId -> Kdmid.Query.checkAppointments requestId
                | Get.Request.MidpassCheckStatus number -> Midpass.Query.checkStatus number
                |> fun createResponse -> deps |> createResponse |> deps.sendResult
            | Post post ->
                match post with
                | Post.Request.KdmidSubscribe model -> Kdmid.Command.subscribe model >> deps.sendResult
                | Post.Request.KdmidCheckAppointments model -> Kdmid.Command.checkAppointments model >> deps.sendResult
                | Post.Request.KdmidSendAppointments model -> Kdmid.Command.sendAppointments model >> deps.sendResults
                | Post.Request.KdmidConfirmAppointment model ->
                    Kdmid.Command.confirmAppointment model >> deps.sendResult
                |> fun send -> deps |> send)
