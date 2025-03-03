module EA.Telegram.Services.Consumer.Culture.Query

open System
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram.Endpoints.Request
open EA.Telegram.Endpoints.Culture
open EA.Telegram.Dependencies.Consumer

let private createMessage chatId msgIdOpt nameOpt data =
    match data |> Seq.length with
    | 0 -> Text.create "No data"
    | _ ->
        ButtonsGroup.create
            { Name = nameOpt |> Option.defaultValue "Choose what do you want"
              Columns = 1
              Buttons =
                data
                |> Seq.map (fun (callback, name) -> callback |> CallbackData |> Button.create name)
                |> Set.ofSeq }
    |> Message.tryReplace msgIdOpt chatId

let getCultures () =
    fun (deps: Culture.Dependencies) ->
        deps.getAvailableCultures ()
        |> ResultAsync.map (
            Seq.map (fun culture ->
                let route = culture.Key |> Post.SetCulture |> Request.Post |> Culture
                let name = culture.Value

                route.Value, name)
        )
        |> ResultAsync.map (createMessage deps.ChatId None (Some "Choose the language"))

let getCulturesCallback callback =
    fun (deps: Culture.Dependencies) ->
        deps.getAvailableCultures ()
        |> ResultAsync.map (
            Seq.map (fun culture ->
                let route =
                    (callback, culture.Key) |> Post.SetCultureCallback |> Request.Post |> Culture

                let name = culture.Value

                (route.Value, name))
        )
        |> ResultAsync.map (createMessage deps.ChatId None (Some "Choose the language"))
