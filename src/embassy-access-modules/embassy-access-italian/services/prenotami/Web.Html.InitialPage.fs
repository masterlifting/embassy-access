module EA.Italian.Services.Prenotami.Web.Html.InitialPage

open System
open System.Text.RegularExpressions
open Infrastructure.Prelude
open Web.Clients.Domain.Browser
open EA.Italian.Services.Domain.Prenotami

let parse (credentials: Credentials) =
    fun (loadPage, waitPage, fillInput, clickButton, mouseShuffle) ->
        // pipe
        loadPage ("https://prenotami.esteri.it" |> Uri)
        |> ResultAsync.bindAsync mouseShuffle
        |> ResultAsync.bindAsync (fillInput (Selector "//input[@id='login-email']") credentials.Login)
        |> ResultAsync.bindAsync mouseShuffle
        |> ResultAsync.bindAsync (fillInput (Selector "//input[@id='login-password']") credentials.Password)
        |> ResultAsync.bindAsync mouseShuffle
        |> ResultAsync.bindAsync (loadPage ("https://www.google.com/recaptcha/enterprise/reload?k=6LdkwrIqAAAAAC4NX-g_j7lEx9vh1rg94ZL2cFfY" |> Uri))
        |> ResultAsync.bindAsync (loadPage ("https://www.google.com/recaptcha/enterprise/clr?k=6LdkwrIqAAAAAC4NX-g_j7lEx9vh1rg94ZL2cFfY" |> Uri))
        |> ResultAsync.bindAsync (clickButton (Selector "//button[@type='submit' and contains(@class, 'g-recaptcha')]"))
        |> ResultAsync.bindAsync (waitPage (Regex(".*UserArea$")))
