module EA.Telegram.Features.Embassies.Russian.Midpass.Command

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Russian.Services.Domain.Midpass
open EA.Telegram.Router.Embassies.Russian
open EA.Telegram.Features.Dependencies.Embassies.Russian

let handleProcessResult (_: Request<Payload>) =
    fun (_: Midpass.Dependencies) ->
        $"Spreading messages is not implemented yet."
        |> NotImplemented
        |> Error
        |> async.Return

let checkStatus (serviceId: ServiceId) (embassyId: EmbassyId) (number: string) =
    fun (_: Midpass.Dependencies) ->
        $"Checking status for service {serviceId.Value} at embassy {embassyId.Value} with number {number} is not implemented."
        + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
        |> async.Return

let deleteRequest requestId =
    fun (deps: Midpass.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.deleteRequest requestId)
        |> ResultAsync.map (fun _ ->
            $"Subscription with ID '%s{requestId.Value}' has been deleted successfully."
            |> Text.create
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)
