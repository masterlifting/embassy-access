module EA.Italian.Services.Prenotami.Web.Html.InitialPage

open System
open Infrastructure.Prelude
open Web.Clients.Domain.Browser
open EA.Italian.Services.Domain.Prenotami

let parse (credentials: Credentials) =
    fun (loadPage, fillInput, mouseShuffle, executeCommand) ->
        // pipe
        loadPage ("https://prenotami.esteri.it" |> Uri)
        |> ResultAsync.bindAsync (fillInput (Selector "//input[@id='login-email']") credentials.Login)
        |> ResultAsync.bindAsync (fillInput (Selector "//input[@id='login-password']") credentials.Password)
        |> ResultAsync.bindAsync mouseShuffle
        |> ResultAsync.bindAsync (executeCommand (Selector "form#login-form") "form => form.submit()")
