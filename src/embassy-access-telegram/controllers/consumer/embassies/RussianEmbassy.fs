[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.RussianEmbassy

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Dependencies.Consumer.Embassies
open EA.Telegram.Services.Consumer.Embassies.RussianEmbassy
open EA.Telegram.Endpoints.Consumer.Embassies.RussianEmbassy

let respond request =
    fun (deps: Consumer.Dependencies) ->
        RussianEmbassy.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Post post ->
                match post with
                | KdmidSubscribe model -> deps |> Kdmid.subscribe model
                | KdmidCheckAppointments model -> deps |> Kdmid.checkAppointments model
                | KdmidSendAppointments model -> deps |> Kdmid.sendAppointments model
                | KdmidConfirmAppointment model -> deps |> Kdmid.confirmAppointment model
                | MidpassCheckStatus model -> deps |> Midpass.checkStatus model)
