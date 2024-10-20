[<RequireQualifiedAccess>]
module EA.Telegram.Responses.Response

open Infrastructure
open Web.Telegram.Domain.Producer

let create<'a> (chatId, msgId) (value: 'a) =
    { Id = msgId
      ChatId = chatId
      Value = value }

let createText (chatId, msgId) (value: string) =
    { Id = msgId
      ChatId = chatId
      Value = value }
    |> Text

let createButtons (chatId, msgId) (value: Buttons) =
    { Id = msgId
      ChatId = chatId
      Value = value }
    |> Buttons

let Ok client ct =
    ResultAsync.bindAsync (fun data ->
        client
        |> Web.Telegram.Client.Producer.produce ct data
        |> ResultAsync.map (fun _ -> ()))

let Result chatId client ct =
    fun (dataRes: Async<Result<Data, Error'>>) ->
        async {
            match! dataRes with
            | Ok data ->
                return!
                    client
                    |> Web.Telegram.Client.Producer.produce ct data
                    |> ResultAsync.map (fun _ -> ())
            | Error error ->
                let data = error.Message |> create (chatId, New) |> Text

                match! client |> Web.Telegram.Client.Producer.produce ct data with
                | Ok _ -> return Error error
                | Error error -> return Error error
        }
