module EA.Italian.Services.Prenotami.Web.Html.InitialPage

open System
open Infrastructure.Prelude
open Web.Clients.Domain.Browser
open EA.Italian.Services.Domain.Prenotami

let parse (credentials: Credentials) =
    fun (loadBrowserPage, fillBrowserForm, clickBrowserButton) ->
        // pipe
        loadBrowserPage ("https://prenotami.esteri.it" |> Uri)
        |> ResultAsync.bindAsync (fillBrowserForm (Selector "//input[@id='login-email']") credentials.Login)
        |> ResultAsync.bindAsync (fillBrowserForm (Selector "#login-password") credentials.Password)
        |> ResultAsync.bindAsync (clickBrowserButton (Selector "#btnLogin"))
