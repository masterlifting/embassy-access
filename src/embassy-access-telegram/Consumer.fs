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

    type MineCmdType = CancellationToken -> IConfigurationRoot -> ChatId -> Async<Result<Producer.Data, Error'>>

    let private (|StartCmd|MineCmd|AskCmd|NoCmd|ErrorCmd|) value =
        match value with
        | AP.IsString value ->
            match value with
            | "/start" -> StartCmd Message.Create.Buttons.embassies
            | "/mine" -> MineCmd Message.Create.Buttons.chatEmbassies
            | "/ask" -> AskCmd("/ask command" |> NotSupported)
            | _ -> NoCmd value
        | _ -> ErrorCmd(NotSupported value)

    // let data: PayloadResponse =
    //                     { Config = cfg
    //                       Ct = ct
    //                       ChatId = msg.ChatId
    //                       Embassy = embassy
    //                       Country = country
    //                       City = city
    //                       Payload = payload }
    let private (|SubscribeCallback|InformationCallback|NoCallback|) (value: string) =
        let data = value.Split '|'

        if data.Length < 2 then
            NoCallback value
        else
            match data[0] with
            | "SUBSCRIBE" ->
                match data.Length - 1 with
                | 1 ->
                    let embassy = data[1]
                    let cmd = Message.Create.Buttons.countries embassy
                    SubscribeCallback cmd
                | 2 ->
                    let embassy = data[1]
                    let country = data[2]
                    let cmd = Message.Create.Buttons.cities (embassy, country)
                    SubscribeCallback cmd
                | _ -> NoCallback value
            | "INFO" ->
                match data.Length - 1 with
                | 1 ->
                    let embassy = data[1]
                    let cmd = Message.Create.Buttons.chatCountries embassy
                    InformationCallback cmd
                | 2 ->
                    let embassy = data[1]
                    let country = data[2]
                    let cmd = Message.Create.Buttons.chatCities (embassy, country)
                    InformationCallback cmd
                | _ -> NoCallback value
            | _ -> NoCallback value

    let private (|SubscribeAction|InfoAction|NoAction|) (value: string) =
        let data = value.Split '|'

        if data.Length < 2 then
            NoAction
        else
            match data[0] with
            | "SUBSCRIBE" ->
                match data.Length - 1 with
                | 3 ->
                    let embassy = data.[1]
                    let country = data.[2]
                    let city = data.[3]
                    let cmd = Message.Create.Text.payloadRequest (embassy, country, city)
                    SubscribeAction cmd
                | _ -> NoAction
            | "INFO" ->
                match data.Length - 1 with
                | 3 ->
                    let embassy = data.[1]
                    let country = data.[2]
                    let city = data.[3]
                    let cmd = Message.Create.Text.listRequests (embassy, country, city)
                    InfoAction cmd
                | _ -> NoAction
            | _ -> NoAction

    let text ct cfg (msg: Consumer.Dto<string>) client =
        async {
            match msg.Value with
            | StartCmd cmd -> return! cmd msg.ChatId |> Respond.Ok ct client
            | MineCmd cmd -> return! cmd msg.ChatId cfg ct |> Respond.Result ct msg.ChatId client
            | AskCmd error -> return! client |> Respond.Error ct msg.ChatId error
            | NoCmd value ->
                match value with
                | SubscribeAction cmd -> return! cmd (msg.ChatId, msg.Id) |> Respond.Ok ct client
                | InfoAction cmd -> return! cmd (msg.ChatId, msg.Id) cfg ct |> Respond.Result ct msg.ChatId client
                | NoAction
                | _ -> return Error <| NotSupported $"Text: {msg.Value}."
            | ErrorCmd error -> return! client |> Respond.Error ct msg.ChatId error
            | _ -> return Error <| NotSupported $"Text: {msg.Value}."
        }

    let callback ct cfg (msg: Consumer.Dto<string>) client =
        async {
            match msg.Value with
            | SubscribeCallback cmd -> return! cmd (msg.ChatId, msg.Id) |> Respond.Ok ct client
            | InformationCallback cmd -> return! cmd (msg.ChatId, msg.Id) cfg ct |> Respond.Result ct msg.ChatId client
            | NoCallback _
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
        | _ -> $"{data}." |> NotSupported |> Error |> async.Return

let start ct cfg =
    Domain.Key.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> EnvKey
    |> Web.Telegram.Client.create
    |> Result.map (fun client -> Web.Domain.Consumer.Telegram(client, handle ct cfg client))
    |> Web.Client.consume ct
