module EA.Telegram.Services.Consumer.Culture.Service

open System
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Endpoints.Consumer

let private createButtons chatId msgIdOpt buttonGroupName data =
    match data |> Seq.length with
    | 0 -> Text.create "No data"
    | _ ->
        Buttons.create
            { Name = buttonGroupName |> Option.defaultValue "Choose what do you want"
              Columns = 1
              Data = data |> Map.ofSeq }
    |> fun mapToData -> (chatId, msgIdOpt |> Option.map Replace |> Option.defaultValue New) |> mapToData

module internal Query =

    let getCultures () =
        fun (deps: Culture.Dependencies) ->
            deps.getSystemCultures ()
            |> ResultAsync.map (createButtons deps.ChatId None (Some "Choose the language"))
