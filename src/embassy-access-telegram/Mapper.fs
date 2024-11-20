[<RequireQualifiedAccess>]
module EA.Telegram.Mapper

open Infrastructure
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Telegram.Domain

module Chat =
    let toExternal chat =
        let result = External.Chat()

        result.Id <- chat.Id.Value

        result.Subscriptions <-
            chat.Subscriptions
            |> Seq.map (function
                | RequestId id -> string id)
            |> Seq.toList

        result

    let toInternal (chat: External.Chat) =
        chat.Subscriptions
        |> Seq.map (fun x ->
            match x with
            | AP.IsGuid id -> Ok <| RequestId id
            | _ -> Error <| NotSupported $"Chat subscription {x}.")
        |> Result.choose
        |> Result.map Set.ofList
        |> Result.map (fun subscriptions ->
            { Id = chat.Id |> ChatId
              Subscriptions = subscriptions })
