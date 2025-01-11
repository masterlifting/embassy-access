[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Producer.Core

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Dependencies.Producer
open EA.Telegram.Endpoints.Consumer.Request
open EA.Telegram.Endpoints.Consumer.Embassies.Russian
open EA.Embassies.Russian.Kdmid.Domain.Payload

let toAppointments (embassy: EmbassyNode, appointments: Set<Appointment>) =
    fun (deps: Core.Dependencies) ->
        let idParts = embassy.Id.Value |> Graph.split
        match idParts.Length > 1 with
        | false ->
            $"Embassy '{embassy.Name}'"
            |> NotSupported
            |> Error
            |> async.Return
        | true ->
            match idParts[1] with
            | "RU" ->
                EA.Telegram.Dependencies.Consumer.Embassies.Russian.Dependencies.create deps
                deps.RussianDeps |> Russian.Get.toResponse embassyId serviceNode.Value
            | _ ->
                $"Embassy '{embassy.Name}'"
                |> NotSupported
                |> Error
                |> async.Return
        
let toConfirmations (requestId: RequestId, embassy: EmbassyNode, confirmations: Set<Confirmation>) =
    fun (deps: Core.Dependencies) ->
        deps.getSubscriptionChats requestId
        |> ResultAsync.map (
            List.map (fun chat ->
                confirmations
                |> Seq.map (fun confirmation -> $"'{embassy.ShortName}'. Confirmation: {confirmation.Description}")
                |> String.concat "\n"
                |> fun msg -> (chat.Id, New) |> Text.create msg)
        )

let toError (requestId: RequestId, error: Error') =
    fun (deps: Core.Dependencies) ->
        deps.getSubscriptionChats requestId
        |> ResultAsync.map (List.map (fun chat -> Web.Telegram.Producer.Text.createError error chat.Id))
