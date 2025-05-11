module EA.Telegram.Services.Culture.Query

open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Telegram.Router
open EA.Telegram.Router.Culture
open EA.Telegram.Dependencies

let private createMessage chatId msgIdOpt nameOpt data =
    let name = nameOpt |> Option.defaultValue "Choose from the list"

    match data |> Seq.length with
    | 0 -> Text.create $"No data for the '{name}'"
    | _ ->
        ButtonsGroup.create {
            Name = name
            Columns = 1
            Buttons =
                data
                |> Seq.map (fun (callback, name) -> callback |> CallbackData |> Button.create name)
                |> Set.ofSeq
        }
    |> Message.tryReplace msgIdOpt chatId

let getCultures () =
    fun (deps: Request.Dependencies) ->
        deps.getAvailableCultures ()
        |> Seq.map (fun culture ->
            let route = Router.Culture(Method.Post(Post.SetCulture(culture.Key)))
            let name = culture.Value

            route.Value, name)
        |> createMessage deps.ChatId None (Some "Choose your language")

let getCulturesCallback callback =
    fun (deps: Request.Dependencies) ->
        deps.getAvailableCultures ()
        |> Seq.map (fun culture ->
            let route =
                Router.Culture(Method.Post(Post.SetCultureCallback(callback, culture.Key)))
            let name = culture.Value

            (route.Value, name))
        |> createMessage deps.ChatId None (Some "Choose your language")
