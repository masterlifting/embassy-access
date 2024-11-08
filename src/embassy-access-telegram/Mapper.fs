[<RequireQualifiedAccess>]
module EA.Telegram.Mapper

open System
open Infrastructure
open EA.Core.Domain
open EA.Telegram.Domain

module Chat =
    let toExternal chat =
        let result = External.Chat()
        result.Id <- chat.Id.Value

        result.Subscriptions <-
            chat.Subscriptions
            |> Seq.map (function
                | RequestId x -> x |> string)
            |> Seq.toList

        result

    let toInternal (chat: External.Chat) =
        chat.Subscriptions
        |> Seq.map (fun x ->
            match Guid.TryParse x with
            | true, guid -> Ok <| RequestId guid
            | _ -> Error <| NotSupported $"Chat subscription {x}.")
        |> Result.choose
        |> Result.map Set.ofList
        |> Result.map (fun subscriptions ->
            { Id = chat.Id |> Web.Telegram.Domain.ChatId
              Subscriptions = subscriptions })
