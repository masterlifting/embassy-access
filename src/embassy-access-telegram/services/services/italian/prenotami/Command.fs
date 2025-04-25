module EA.Telegram.Services.Services.Italian.Prenotami.Command

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router.Services.Italian
open EA.Telegram.Dependencies.Services.Italian

let checkSlotsNow (serviceId: ServiceId) (embassyId: EmbassyId) (login: string) (password: string) =
    fun (deps: Prenotami.Dependencies) ->
        $"Checking slots now for service {serviceId.ValueStr} at embassy {embassyId.ValueStr} with credentials {login}..."
        |> Text.create
        |> Message.createNew deps.ChatId
        |> Ok
        |> async.Return

let slotsAutoNotification (serviceId: ServiceId) (embassyId: EmbassyId) (login: string) (password: string) =
    fun (deps: Prenotami.Dependencies) ->
        $"Auto notification for slots enabled for service {serviceId.ValueStr} at embassy {embassyId.ValueStr} with credentials {login}."
        |> Text.create
        |> Message.createNew deps.ChatId
        |> Ok
        |> async.Return
