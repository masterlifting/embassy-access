module EA.Italian.Services.Prenotami.Web.Html

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Browser
open Web.Clients.Domain.Http
open EA.Italian.Services.Domain.Prenotami

let private getCaptchaSiteKey page =
    Html.load page
    |> Result.bind (Html.getNodes "//script[contains(@src, 'recaptcha/enterprise.js')]")
    |> Result.toOption
    |> Option.bind id
    |> Option.bind (fun nodes ->
        nodes
        |> Seq.tryPick (fun node ->
            node
            |> Html.getAttributeValue "src"
            |> Result.toOption
            |> Option.bind id
            |> Option.bind (fun src ->
                if src.Contains "render=" then
                    let startIdx = src.IndexOf("render=") + 7
                    let endIdx = src.IndexOf("&", startIdx)
                    let value =
                        if endIdx > 0 then
                            src.Substring(startIdx, endIdx - startIdx)
                        else
                            src.Substring(startIdx)
                    Some value
                else
                    None)))
    |> function
        | Some siteKey -> Ok siteKey
        | None -> Error <| NotFound "Recaptcha siteKey of 'Prenotami Initial Page' not found."

let private getServiceState page =
    Html.load page
    |> Result.bind (Html.getNodes "//div[@id='serviceState']")
    |> Result.toOption
    |> Option.bind id
    |> Option.bind (fun nodes ->
        nodes
        |> Seq.tryPick (fun node ->
            node
            |> Html.getAttributeValue "data-service-state"
            |> Result.toOption
            |> Option.bind id))
    |> function
        | Some state -> Ok state
        | None -> Error <| NotFound "Service state of 'Prenotami Initial Page' not found."

let processWeb () =
    fun (client: ClientNew) ->
        client.getInitialPage ()
        |> ResultAsync.bind client.setSessionCookie
        |> ResultAsync.bind (fun response -> getCaptchaSiteKey response.Content)
        |> ResultAsync.bindAsync client.solveCaptcha
        |> ResultAsync.map client.buildFormData
        |> ResultAsync.bindAsync client.postLoginPage
        |> ResultAsync.bind client.setAuthCookie
        |> ResultAsync.bindAsync client.getServicePage
        |> ResultAsync.bind (fun response -> getServiceState response.Content)
