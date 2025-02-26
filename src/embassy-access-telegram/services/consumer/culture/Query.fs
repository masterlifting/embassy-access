module EA.Telegram.Services.Consumer.Culture.Query

open System
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram.Endpoints.Request
open EA.Telegram.Endpoints.Culture
open EA.Telegram.Dependencies.Consumer

let private createButtons chatId msgIdOpt buttonGroupName data =
    match data |> Seq.length with
    | 0 -> Text.create "No data"
    | _ ->
        ButtonsGroup.create
            { Name = buttonGroupName |> Option.defaultValue "Choose what do you want"
              Columns = 1
              Items = data |> Map.ofSeq }
    |> fun mapToData -> (chatId, msgIdOpt |> Option.map Replace |> Option.defaultValue New) |> mapToData

let getCultures () =
    fun (deps: Culture.Dependencies) ->
        deps.getAvailableCultures ()
        |> ResultAsync.map (
            Seq.map (fun culture ->
                let route = culture.Key |> Post.SetCulture |> Request.Post |> Culture
                let name = culture.Value

                route.Value |> Text, name)
        )
        |> ResultAsync.map (createButtons deps.ChatId None (Some "Choose the language"))

let getCulturesCallback callback =
    fun (deps: Culture.Dependencies) ->
        deps.getAvailableCultures ()
        |> ResultAsync.map (
            Seq.map (fun culture ->
                let route =
                    (callback, culture.Key)
                    |> Post.SetCultureCallback
                    |> Request.Post
                    |> Culture

                let name = culture.Value

                (route.Value, name))
        )
        |> ResultAsync.map (createButtons deps.ChatId None (Some "Choose the language"))
