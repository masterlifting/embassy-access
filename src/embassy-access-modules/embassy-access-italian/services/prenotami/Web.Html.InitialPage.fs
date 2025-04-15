﻿module EA.Italian.Services.Prenotami.Web.Html.InitialPage

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Parser
open Web.Clients.Domain.Http
open EA.Italian.Services.Prenotami.Web

let private createHttpRequest queryParams = {
    Path = "/queue/orderinfo.aspx?" + queryParams
    Headers = None
}

let private parseHttpResponse page =
    Html.load page
    |> Result.bind Common.pageHasError
    |> Result.bind (Html.getNodes "//input | //img")
    |> Result.bind (function
        | None -> Error <| NotFound "Nodes on the Initial Page not found."
        | Some nodes ->
            nodes
            |> Seq.choose (fun node ->
                match node.Name with
                | "input" ->
                    match node |> Html.getAttributeValue "name", node |> Html.getAttributeValue "value" with
                    | Ok(Some name), Ok(Some value) -> Some(name, value)
                    | Ok(Some name), Ok(None) -> Some(name, String.Empty)
                    | _ -> None
                | "img" ->
                    match node |> Html.getAttributeValue "src" with
                    | Ok(Some code) when code.Contains "CodeImage" -> Some("captchaUrlPath", code)
                    | _ -> None
                | _ -> None)
            |> Map.ofSeq
            |> Ok)
    |> Result.bind (fun result ->
        let requiredKeys =
            Set [
                "captchaUrlPath"
                "__VIEWSTATE"
                "__EVENTVALIDATION"
                "ctl00$MainContent$txtID"
                "ctl00$MainContent$txtUniqueID"
                "ctl00$MainContent$ButtonA"
            ]

        let notRequiredKeys = Set [ "__VIEWSTATEGENERATOR" ]

        let requiredResult = result |> Map.filter (fun key _ -> requiredKeys.Contains key)

        let notRequiredResult =
            result |> Map.filter (fun key _ -> notRequiredKeys.Contains key)

        match requiredKeys.Count = requiredResult.Count with
        | true -> Ok(requiredResult |> Map.combine <| notRequiredResult)
        | false -> Error <| NotFound "Headers of 'Kdmid Initial Page' not found.")

let private createCaptchaRequest urlPath = {
    Path = $"/queue/%s{urlPath}"
    Headers = None
}

let private prepareHttpFormData pageData =
    pageData
    |> Map.remove "captchaUrlPath"
    |> Map.add "ctl00$MainContent$FeedbackClientID" "0"
    |> Map.add "ctl00$MainContent$FeedbackOrderID" "0"

let parse () =
    fun (httpClient, getInitialPage) ->
        // define
        let getRequest = "" |> createHttpRequest |> getInitialPage
        let setCookie = ResultAsync.bind (httpClient |> Http.setRequiredCookie)
        let parseResponse = ResultAsync.bind parseHttpResponse

        // pipe
        httpClient
        |> getRequest
        |> setCookie
        |> parseResponse
        |> ResultAsync.map (prepareHttpFormData >> Http.buildFormData)
