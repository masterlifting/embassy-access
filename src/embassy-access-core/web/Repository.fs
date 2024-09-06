[<RequireQualifiedAccess>]
module EmbassyAccess.Web.Repository

open Infrastructure
open Web
open EmbassyAccess.Web
open Infrastructure.Logging

module Request =

    let send ct filter client =
        match client with
        | Client.Type.Telegram context ->
            Log.trace $"Telegram send request with filter \n{filter}"
            context |> TelegramRepository.Request.send ct filter
        | _ -> async { return Error <| NotSupported $"Client {client}" }

    let receive ct response client =
        match client with
        | Client.Type.Telegram context ->
            Log.trace $"Telegram receive request with response \n{response}"
            context |> TelegramRepository.Request.receive ct response
        | _ -> async { return Error <| NotSupported $"Client {client}" }
