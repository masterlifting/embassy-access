module EA.Telegram.Services.Culture.Query

open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram.Router
open EA.Telegram.Router.Culture
open EA.Telegram.Dependencies

let private createMessage chatId msgIdOpt nameOpt data =
    let name = nameOpt |> Option.defaultValue "Choose from the list"

    match data |> Seq.length with
    | 0 -> Text.create $"No data for the {name}"
    | _ ->
        ButtonsGroup.create
            { Name = name
              Columns = 1
              Buttons =
                data
                |> Seq.map (fun (callback, name) -> callback |> CallbackData |> Button.create name)
                |> Set.ofSeq }
    |> Message.tryReplace msgIdOpt chatId

let getCultures () =
    fun (deps: Request.Dependencies) ->
        deps.getAvailableCultures ()
        |> ResultAsync.map (
            Seq.map (fun culture ->
                let route = culture.Key |> Post.SetCulture |> Method.Post |> Router.Culture
                let name = culture.Value

                route.Value, name)
        )
        |> ResultAsync.map (createMessage deps.ChatId None (Some "Choose the language"))

let getCulturesCallback callback =
    fun (deps: Request.Dependencies) ->
        deps.getAvailableCultures ()
        |> ResultAsync.map (
            Seq.map (fun culture ->
                let route =
                    (callback, culture.Key)
                    |> Post.SetCultureCallback
                    |> Method.Post
                    |> Router.Culture

                let name = culture.Value

                (route.Value, name))
        )
        |> ResultAsync.map (createMessage deps.ChatId None (Some "Choose the language"))
