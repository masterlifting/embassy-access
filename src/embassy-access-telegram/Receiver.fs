module internal EmbassyAccess.Telegarm.Receiver

open Infrastructure
open Web.Telegram.Domain.Receive
open EmbassyAccess


module private Sender =
    open Web.Telegram.Domain.Send

    let sendEmbassies ct messageId chatId =
        let data =
            Api.getEmbassies ()
            |> Seq.concat
            |> Seq.map Mapper.Embassy.toExternal
            |> Seq.map (fun embassy -> embassy.Name, embassy.Name)
            |> Seq.sortBy fst
            |> Map

        let buttons: Buttons =
            { Name = "Available Embassies."
              Columns = 3
              Data = data }

        let message =
            { Id = New
              ChatId = chatId
              Value = buttons }
            |> Buttons

        message |> Web.Telegram.Client.send ct

    let sendCountries ct messageId chatId embassyName =
        let data =
            Api.getEmbassies ()
            |> Seq.concat
            |> Seq.map Mapper.Embassy.toExternal
            |> Seq.filter (fun embassy -> embassy.Name = embassyName)
            |> Seq.map _.Country
            |> Seq.map (fun country -> country.Name, country.Name)
            |> Seq.sortBy fst
            |> Map

        let buttons: Buttons =
            { Name = $"Available Countries for '{embassyName}'."
              Columns = 3
              Data = data }

        let message =
            { Id = messageId |> Replace
              ChatId = chatId
              Value = buttons }
            |> Buttons

        message |> Web.Telegram.Client.send ct

let private receiveText ct client (msg: Message<string>) =
    match msg.Value with
    | "/start" -> client |> Sender.sendEmbassies ct msg.Id msg.ChatId
    | _ -> async { return Error <| NotSupported $"Message text: {msg.Value}" }

let private receiveCallback ct client (msg: Message<string>) =
    match msg.Value with
    | Mapper.Embassy.Russian -> client |> Sender.sendCountries ct msg.Id msg.ChatId Mapper.Embassy.Russian
    | _ -> async { return Error <| NotSupported $"Callback data: {msg.Value}" }

let private receiveDataMessage ct client msg =
    match msg with
    | Text text -> text |> receiveText ct client
    | _ -> async { return Error <| NotSupported $"Message type: {msg}" }

let receive ct client =
    fun data ->
        match data with
        | Message msg -> msg |> receiveDataMessage ct client
        | CallbackQuery msg -> msg |> receiveCallback ct client
        | _ -> async { return Error <| NotSupported $"Data type: {data}" }
        |> ResultAsync.map (fun _ -> ())
