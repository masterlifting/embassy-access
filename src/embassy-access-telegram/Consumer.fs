module EA.Telegram.Consumer

open System
open Infrastructure
open Web.Telegram.Domain

module private Respond =

    let Ok ct client data =
        client
        |> Web.Telegram.Client.Producer.produce ct data
        |> ResultAsync.map (fun _ -> ())

    let Error ct chatId (error: Error') client =
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
            | Error error -> client |> Error ct chatId error)

module private Consume =

    let private (|SupportedEmbassies|UserEmbassies|SupportInstruction|NoMenu|) value =
        match value with
        | "/start" -> SupportedEmbassies(Message.Create.Buttons.embassies ())
        | "/mine" -> UserEmbassies(Message.Create.Buttons.userEmbassies ())
        | "/ask" -> SupportInstruction("/ask command" |> NotSupported)
        | _ -> NoMenu

    let private (|SupportedCountries|SupportedCities|UserCountries|UserCities|NoCallback|) (value: string) =
        let data = value.Split '|'

        if data.Length < 2 then
            NoCallback
        else
            match data[0] with
            | "SUBSCRIBE" ->
                match data.Length - 1 with
                | 1 ->
                    let embassy = data[1]
                    let cmd = Message.Create.Buttons.countries embassy
                    SupportedCountries cmd
                | 2 ->
                    let embassy = data[1]
                    let country = data[2]
                    let cmd = Message.Create.Buttons.cities (embassy, country)
                    SupportedCities cmd
                | _ -> NoCallback
            | "INFO" ->
                match data.Length - 1 with
                | 1 ->
                    let embassy = data[1]
                    let cmd = Message.Create.Buttons.userCountries embassy
                    UserCountries cmd
                | 2 ->
                    let embassy = data[1]
                    let country = data[2]
                    let cmd = Message.Create.Buttons.userCities (embassy, country)
                    UserCities cmd
                | _ -> NoCallback
            | _ -> NoCallback

    let private (|SubscriptionResult|UserSubscriptions|NoText|) (value: string) =
        let data = value.Split '|'

        if data.Length < 2 then
            NoText
        else
            match data[0] with
            | "SUBSCRIBE" ->
                match data.Length - 1 with
                | 4 ->
                    let embassy = data[1]
                    let country = data[2]
                    let city = data[3]
                    let payload = data[4]
                    let cmd = Message.Create.Text.subscribe (embassy, country, city, payload)
                    SubscriptionResult cmd
                | _ -> NoText
            | "INFO" ->
                match data.Length - 1 with
                | 3 ->
                    let embassy = data.[1]
                    let country = data.[2]
                    let city = data.[3]
                    let cmd = Message.Create.Text.userRequests (embassy, country, city)
                    UserSubscriptions cmd
                | _ -> NoText
            | _ -> NoText

    let text ct cfg (msg: Consumer.Dto<string>) client =
        async {
            match msg.Value with
            | SupportedEmbassies cmd -> return! cmd msg.ChatId |> Respond.Ok ct client
            | UserEmbassies cmd -> return! cmd msg.ChatId cfg ct |> Respond.Result ct msg.ChatId client
            | SupportInstruction error -> return! client |> Respond.Error ct msg.ChatId error
            | SubscriptionResult cmd -> return! cmd msg.ChatId cfg ct |> Respond.Result ct msg.ChatId client
            | UserSubscriptions cmd -> return! cmd (msg.ChatId, msg.Id) cfg ct |> Respond.Result ct msg.ChatId client
            | NoMenu
            | NoText
            | _ -> return Error <| NotSupported $"Text: {msg.Value}."
        }

    let callback ct cfg (msg: Consumer.Dto<string>) client =
        async {
            match msg.Value with
            | SupportedCountries cmd -> return! cmd (msg.ChatId, msg.Id) |> Respond.Ok ct client
            | SupportedCities cmd -> return! cmd (msg.ChatId, msg.Id) |> Respond.Ok ct client
            | UserCountries cmd -> return! cmd (msg.ChatId, msg.Id) cfg ct |> Respond.Result ct msg.ChatId client
            | UserCities cmd -> return! cmd (msg.ChatId, msg.Id) cfg ct |> Respond.Result ct msg.ChatId client
            | NoCallback
            | _ -> return Error <| NotSupported $"Callback: {msg.Value}."
        }

let private handle ct cfg client =
    fun data ->
        match data with
        | Consumer.Message message ->
            match message with
            | Consumer.Text dto -> client |> Consume.text ct cfg dto
            | _ -> $"%A{message}" |> NotSupported |> Error |> async.Return
        | Consumer.CallbackQuery dto -> client |> Consume.callback ct cfg dto
        | _ -> $"%A{data}." |> NotSupported |> Error |> async.Return

let start ct cfg =
    Domain.Key.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> EnvKey
    |> Web.Telegram.Client.create
    |> Result.map (fun client -> Web.Domain.Consumer.Telegram(client, handle ct cfg client))
    |> Web.Client.consume ct
