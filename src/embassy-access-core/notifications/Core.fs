[<RequireQualifiedAccess>]
module internal EmbassyAccess.Notifications.Core

open Infrastructure

let private createBot () =
    Configuration.getEnvVar "TelegramBotToken"
    |> Result.bind (Option.map Ok >> Option.defaultValue (Error <| NotFound "Telegram bot token"))
    |> Result.map Web.Domain.Telegram
    |> Result.bind Web.Client.create

let sendAppointments client (request: EmbassyAccess.Domain.Request) =
    async { return Error <| NotImplemented $"Send appointments with request Id {request.Id}" }

let sendConfirmations client (request: EmbassyAccess.Domain.Request) =
    async { return Error <| NotImplemented $"Send confirmations with request Id {request.Id}" }
