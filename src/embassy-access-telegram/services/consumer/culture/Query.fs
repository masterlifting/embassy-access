module EA.Telegram.Services.Consumer.Culture.Query

open System
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram.Endpoints.Culture
open EA.Telegram.Dependencies.Consumer

let private createButtons chatId msgIdOpt buttonGroupName data =
    match data |> Seq.length with
    | 0 -> Text.create "No data"
    | _ ->
        Buttons.create
            { Name = buttonGroupName |> Option.defaultValue "Choose what do you want"
              Columns = 1
              Data = data |> Map.ofSeq }
    |> fun mapToData -> (chatId, msgIdOpt |> Option.map Replace |> Option.defaultValue New) |> mapToData

let getCultures () =
    fun (deps: Culture.Dependencies) ->
        deps.getSystemCultures ()
        |> ResultAsync.map (
            Seq.map (fun (name, displayName) ->
                let route: Post.Model.Culture =
                    { Post.Model.Culture.Name = displayName
                      Post.Model.Culture.Code = name }
                    |> Post.SetCulture
                    |> Culture.Post
                    |> Request.Post
                    |> Culture

                (route.Value, displayName))
        )
        |> ResultAsync.map (createButtons deps.ChatId None (Some "Choose the language"))
