module EA.Telegram.CommandHandler.Russian

open Infrastructure
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram
open EA.Embassies.Russian.Domain

let service (embassy, nameOpt, index) =
    fun (chatId, msgId) ->

        let inline createButtons index items =
            items
            |> Seq.map (fun item -> (embassy, item, index) |> Command.RussianService |> Command.set, item)
            |> Map
            |> fun data ->
                { Buttons.Name = "Какую услугу вы хотите получить?"
                  Columns = 1
                  Data = data }
                |> Buttons.create (chatId, msgId |> Replace)

        match nameOpt with
        | None -> Service.LIST |> createButtons index
        | Some service ->
            match service |> Service.getNext index with
            | [] ->
                let command = (embassy, service) |> Command.ServiceGet |> Command.set
                $"Use '{command}' to get the service" |> Text.create (chatId, msgId |> Replace)
            | items -> items |> createButtons (index + 1)
        |> Ok
        |> async.Return
