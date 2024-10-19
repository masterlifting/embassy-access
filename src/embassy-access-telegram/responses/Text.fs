[<RequireQualifiedAccess>]
module EA.Telegram.Responses.Text

open System
open Infrastructure
open Web.Telegram.Domain.Producer
open EA.Telegram.Persistence
open EA.Telegram.Responses
open EA.Telegram.Domain.Response
open EA.Domain


module Create =

    let error (error: Error') =
        fun chatId -> error.Message |> Response.create (chatId, New) |> Text

    let confirmation (embassy, (confirmations: Set<Confirmation>)) =
        fun chatId ->
            confirmations
            |> Seq.map _.Description
            |> String.concat "\n"
            |> Response.create (chatId, New)
            |> Text

    let subscriptionRequest (embassy', country', city') =
        fun (chatId, msgId) ->
            match (embassy', country', city') |> EA.Mapper.Embassy.createInternal with
            | Error error -> error.Message
            | Ok embassy ->
                match embassy with
                | Russian _ ->
                    $"Send your payload using the following format: '{StartCtx}|{embassy'}|{country'}|{city'}|your_link_here'."
                | _ -> $"Not supported embassy: '{embassy'}'."
            |> Response.create (chatId, msgId |> Replace)
            |> Text
            |> Ok
            |> async.Return

    let userSubscriptions (embassy', country', city') =
        fun (chatId, msgId) cfg ct ->
            EA.Mapper.Embassy.createInternal (embassy', country', city')
            |> ResultAsync.wrap (fun embassy ->
                Storage.Chat.create cfg
                |> ResultAsync.wrap (Repository.Query.Chat.tryFind ct chatId)
                |> ResultAsync.bindAsync (function
                    | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                    | Some chat ->
                        Storage.Request.create cfg
                        |> ResultAsync.wrap (Repository.Query.Request.getRequests ct chat))
                |> ResultAsync.map (Seq.filter (fun request -> request.Embassy = embassy))
                |> ResultAsync.map (fun requests ->
                    requests
                    |> Seq.map (fun request -> $"{request.Id} -> {request.Payload}")
                    |> String.concat Environment.NewLine
                    |> Response.create (chatId, msgId |> Replace)
                    |> Text))

    let subscribe (embassy', country', city', payload) =
        fun chatId cfg ct ->
            (embassy', country', city')
            |> EA.Mapper.Embassy.createInternal
            |> ResultAsync.wrap (fun embassy ->
                match embassy with
                | Russian _ ->

                    let createOrUpdatePassportSearchRequest ct =
                        Storage.Request.create cfg
                        |> ResultAsync.wrap (
                            Repository.Command.Request.createOrUpdatePassportSearch ct (embassy, payload)
                        )

                    let createOrUpdateChatSubscription ct =
                        ResultAsync.bindAsync (fun (request: Request) ->
                            Storage.Chat.create cfg
                            |> ResultAsync.wrap (
                                Repository.Command.Chat.createOrUpdateSubscription ct (chatId, request.Id)
                            )
                            |> ResultAsync.map (fun _ -> request))

                    createOrUpdatePassportSearchRequest ct
                    |> createOrUpdateChatSubscription ct
                    |> ResultAsync.map (fun request -> $"Subscription has been activated for '{request.Embassy}'.")
                    |> ResultAsync.map (Response.create (chatId, New) >> Text)
                | _ -> embassy' |> NotSupported |> Error |> async.Return)
