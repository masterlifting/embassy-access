[<RequireQualifiedAccess>]
module EmbassyAccess.Telegram

open Infrastructure
open Web.Telegram.Domain

let internal AdminChatId = 379444553L

module private Sender =
    open Web.Telegram.Domain.Send

    let send ct msg =
        EnvKey "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"
        |> Web.Telegram.Client.create
        |> ResultAsync.wrap (msg |> Web.Telegram.Client.send ct)

    let sendEmbassies () =
        let embassies =
            Set
                [ nameof Domain.Russian
                  nameof Domain.British
                  nameof Domain.Spanish
                  nameof Domain.Italian
                  nameof Domain.French
                  nameof Domain.German ]

        let buttons: Buttons =
            { Name = "Embassies"
              Data = embassies |> Set.map (fun e -> e, e) |> Map }

        { Id = None
          ChatId = AdminChatId
          Value = buttons }
        |> Buttons

    let sendCountries (embassy: string) =
        let countries =
            match embassy with
            | nameof Domain.Russian -> Set [ nameof Domain.Serbia; nameof Domain.Budapest; nameof Domain.Sarajevo ]
            | _ -> Set.empty

        let buttons: Buttons =
            { Name = "Countries"
              Data = countries |> Set.map (fun c -> c, c) |> Map }

        { Id = None
          ChatId = AdminChatId
          Value = buttons }
        |> Buttons

module private Receiver =
    open Web.Telegram.Domain.Receive

    let private receiveText ct (msg: Message<string>) =
        match msg.Value with
        | "/start" -> Sender.sendEmbassies () |> Sender.send ct |> ResultAsync.bind (fun _ -> Ok())
        | _ -> async { return Error <| NotSupported $"Message text: {msg.Value}" }

    let receiveCallback ct (msg: Message<string>) =
        match msg.Value with
        | "Russian" ->
            Sender.sendCountries "Russian"
            |> Sender.send ct
            |> ResultAsync.bind (fun _ -> Ok())
        | _ -> async { return Error <| NotSupported $"Callback data: {msg.Value}" }

    let receiveDataMessage ct message =
        match message with
        | Text textMsg -> receiveText ct textMsg
        | _ -> async { return Error <| NotSupported $"Message type: {message}" }

let send ct message = message |> Sender.send ct

let receive ct data =
    match data with
    | Receive.Data.Message msg -> msg |> Receiver.receiveDataMessage ct
    | Receive.Data.CallbackQuery msg -> msg |> Receiver.receiveCallback ct
    | _ -> async { return Error <| NotSupported $"Listener type: {data}" }
