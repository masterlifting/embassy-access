[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.Russian

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer.Embassies
open EA.Telegram.Services.Consumer.Embassies.Russian
open EA.Telegram.Endpoints.Consumer.Embassies.Russian

let respond request =
    fun (deps: EA.Telegram.Dependencies.Consumer.Core.Dependencies) ->
        Russian.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Post post ->
                match post with
                | KdmidSubscribe model -> deps |> Kdmid.subscribe model
                | KdmidCheckAppointments model -> deps |> Kdmid.checkAppointments model
                | KdmidSendAppointments model -> deps |> Kdmid.sendAppointments model
                | KdmidConfirmAppointment model -> deps |> Kdmid.confirmAppointment model
                | MidpassCheckStatus model -> deps |> Midpass.checkStatus model)
