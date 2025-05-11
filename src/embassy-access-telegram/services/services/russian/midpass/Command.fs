module EA.Telegram.Services.Services.Russian.Midpass.Command

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router.Services.Russian
open EA.Telegram.Dependencies.Services.Russian

let checkStatus (serviceId: ServiceId) (embassyId: EmbassyId) (number: string) =
    fun (_: Midpass.Dependencies) ->
        $"Checking status for service {serviceId.ValueStr} at embassy {embassyId.ValueStr} with number {number} is not implemented."
        + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
        |> async.Return
let delete requestId =
    fun (deps: Midpass.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.deleteRequest requestId)
        |> ResultAsync.map (fun _ ->
            $"Request with id '%s{requestId.ValueStr}' deleted successfully."
            |> Text.create
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)
