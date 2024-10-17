module EA.Telegram.Consumer

open System
open System.Threading
open Infrastructure
open Microsoft.Extensions.Configuration
open Web.Telegram.Domain
open EA.Telegram.Domain.Message

module private Respond =

    let Ok ct client data =
        client
        |> Web.Telegram.Client.Producer.produce ct data
        |> ResultAsync.map (fun _ -> ())

    let Error ct chatId client (error: Error') =
        error.Message
        |> Message.create (chatId, Producer.DtoId.New)
        |> Producer.Text
        |> Ok ct client
        |> Async.map (function
            | Ok _ -> Error error
            | Error error -> Error error)

    let Result ct chatId client =
        Async.bind (function
            | Ok data -> data |> Ok ct client
            | Error error -> error |> Error ct chatId client)

module private Consume =

    type MineCmdType = CancellationToken -> IConfigurationRoot -> ChatId -> Async<Result<Producer.Data, Error'>>

    let private (|Start|Mine|Ask|None|Error|) value =
        match value with
        | AP.IsString value ->
            match value with
            | "/start" -> Start Message.Create.Buttons.embassies
            | "/mine" -> Mine Message.Create.Buttons.chatEmbassies
            | "/ask" -> Ask("/ask command" |> NotSupported)
            | _ -> None value
        | _ -> Error(NotSupported $"Command: {value}.")
    
    // let data: PayloadResponse =
    //                     { Config = cfg
    //                       Ct = ct
    //                       ChatId = msg.ChatId
    //                       Embassy = embassy
    //                       Country = country
    //                       City = city
    //                       Payload = payload }
    let private respondSubscriptionCallback (data: string array) =
        fun (chatId, msgId) ->
            match data.Length with
            | 1 -> Message.Create.Buttons.countries (chatId, msgId) data[0] |> Some
            | 2 -> Message.Create.Buttons.cities (chatId, msgId) (data.[0], data.[1]) |> Some
            | 3 -> Message.Create.Text.payloadRequest (chatId, msgId) (data.[0], data.[1], data.[2]) |> Some
            | _ -> None
            
    let private respondInformationCallback (data: string array) =
        fun ct cfg ->
            fun (chatId, msgId) ->
                match data.Length with
                | 1 -> Message.Create.Buttons.chatCountries ct cfg (chatId, msgId) data[0] |> Some
                | 2 -> Message.Create.Buttons.chatCities ct cfg (chatId, msgId) (data.[0], data.[1]) |> Some
                | 3 -> Message.Create.Text.listRequests ct cfg (chatId, msgId) (data.[0], data.[1], data.[2]) |> Some
                | _ -> None

    let private (|Subscribe|Information|None|) (value: string) =
        let data = value.Split '|'
        match data with
        | [|"SUBSCRIBE"|] -> Subscribe ( respondSubscriptionCallback data)
        | [|"GET"|] -> Information ( respondInformationCallback data)
        | _ -> None

    let text ct cfg (msg: Consumer.Dto<string>) client =
        async {
            match msg.Value with
            | Start cmd -> return! cmd msg.ChatId |> Respond.Ok ct client
            | Mine cmd -> return! cmd ct cfg msg.ChatId |> Respond.Result ct msg.ChatId client
            | Ask error -> return! error |> Respond.Error ct msg.ChatId client
            | Error error -> return! error |> Respond.Error ct msg.ChatId client
            | None value ->return Error <| NotSupported $"Text: {msg.Value}."
            | _ -> return Error <| NotSupported $"Text: {msg.Value}."
        }

    let callback ct cfg (msg: Consumer.Dto<string>) client =
        async {
            match msg.Value with
            | Subscribe cmd -> return! cmd (msg.ChatId, msg.Id) |> Respond.Ok ct client
            | Information cmd -> return! cmd ct cfg (msg.ChatId, msg.Id) |> Respond.Ok ct client
            | None -> return Error <| NotSupported $"Callback: {msg.Value}."
            | _ -> return Error <| NotSupported $"Callback: {msg.Value}."
        }

let private handle ct cfg client =
    fun data ->
        match data with
        | Consumer.Message message ->
            match message with
            | Consumer.Text dto -> client |> Consume.text ct cfg dto
            | _ -> $"{message}" |> NotSupported |> Error |> async.Return
        | Consumer.CallbackQuery dto -> client |> Consume.callback ct cfg dto
        | _ -> $"Data: {data}." |> NotSupported |> Error |> async.Return

let start ct cfg =
    Domain.Key.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> EnvKey
    |> Web.Telegram.Client.create
    |> Result.map (fun client -> Web.Domain.Consumer.Telegram(client, handle ct cfg client))
    |> Web.Client.consume ct
