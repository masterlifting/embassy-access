[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.RussianEmbassy

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Dependencies.Consumer.Embassies
open EA.Telegram.Services.Consumer.Embassies.RussianEmbassy
open EA.Telegram.Endpoints.Consumer.Embassies.RussianEmbassy

let respond request =
    fun (deps: Consumer.Dependencies) ->
        let sendResult data =
            Web.Telegram.Producer.produceResult data deps.ChatId deps.CancellationToken deps.TelegramBot
            |> ResultAsync.map ignore

        let sendResults data =
            Web.Telegram.Producer.produceResults data deps.ChatId deps.CancellationToken deps.TelegramBot
            |> ResultAsync.map ignore

        RussianEmbassy.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Post post ->
                match post with
                | KdmidSubscribe model -> deps |> Kdmid.subscribe model |> sendResult
                | KdmidCheckAppointments model -> deps |> Kdmid.checkAppointments model |> sendResult
                | KdmidSendAppointments model -> deps |> Kdmid.sendAppointments model |> sendResults
                | KdmidConfirmAppointment model -> deps |> Kdmid.confirmAppointment model |> sendResult
                | MidpassCheckStatus model -> deps |> Midpass.checkStatus model |> sendResult)
