module EA.Telegram.Features.Embassies.Culture.Query

open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Telegram.Dependencies
open EA.Telegram.Features.Dependencies
open EA.Telegram.Router.Culture

let private createMessage chatId msgIdOpt nameOpt buttons =
    let name = nameOpt |> Option.defaultValue "Choose from the list"

    match buttons |> Seq.length with
    | 0 -> Text.create $"No data for the '{name}'"
    | _ ->
        ButtonsGroup.create {
            Name = name
            Columns = 1
            Buttons = buttons |> ButtonsGroup.createButtons
        }
    |> Message.tryReplace msgIdOpt chatId

let private buildRout route = EA.Telegram.Router.Route.Culture route

let getCultures () =
    fun (deps: Request.Dependencies) ->
        let culture = deps.Culture |> Culture.Dependencies.create deps.ct

        culture.getAvailable ()
        |> Seq.map (fun culture ->
            let method = Post(SetCulture culture.Key) |> buildRout
            let name = culture.Value
            name, method.Value)
        |> createMessage deps.ChatId None (Some "Choose your language")

let getCulturesCallback callback =
    fun (deps: Request.Dependencies) ->
        let culture = deps.Culture |> Culture.Dependencies.create deps.ct

        culture.getAvailable ()
        |> Seq.map (fun culture ->
            let method = Post(SetCultureCallback(callback, culture.Key)) |> buildRout
            let name = culture.Value
            name, method.Value)
        |> createMessage deps.ChatId None (Some "Choose your language")
