[<RequireQualifiedAccess>]
module EmbassyAccess.Notification.Repository

open Infrastructure
open Web.Domain
open EmbassyAccess.Notification
open Infrastructure.Logging

module Request =

    let send ct (notification: Send.Request) client =
        match client with
        | Client.Telegram context ->
            Log.trace $"Telegram send request with notification \n{notification}"
            context |> TelegramRepository.Request.send ct notification
        | _ -> async { return Error <| NotSupported $"Client {client}" }

    let receive ct listener client =
        match listener with
        | Listener.Telegram listener ->
            Log.trace $"Telegram receive request with response \n{client}"
            listener |> TelegramRepository.Request.receive ct client
        | _ -> async { return Error <| NotSupported $"Client {listener}" }
