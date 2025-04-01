module EA.Russian.Clients.Kdmid.Web.Html.ValidationPage

open System
open EA.Russian.Clients.Kdmid
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Parser
open Web.Clients.Domain.Http
open EA.Russian.Clients.Domain.Kdmid

let private createHttpRequest queryParams formData =

    let request = {
        Path = $"/queue/orderinfo.aspx?%s{queryParams}"
        Headers = None
    }

    let content: RequestContent =
        String {|
            Data = formData
            Encoding = Text.Encoding.ASCII
            MediaType = "application/x-www-form-urlencoded"
        |}

    request, content

let private pageHasInconsistentState page =
    page
    |> Common.pageHasInconsistentState (function
        | text when text |> String.has "Вы записаны" && not (text |> String.has "список ожидания") ->
            Error
            <| Operation {
                Message = text
                Code = Constants.ErrorCode.REQUEST_AWAITING_LIST |> Custom |> Some
            }
        | text when text |> String.has "Ваша заявка требует подтверждения" ->
            Error
            <| Operation {
                Message = text
                Code = Constants.ErrorCode.REQUEST_NOT_CONFIRMED |> Custom |> Some
            }
        | text when text |> String.has "Заявка удалена" ->
            Error
            <| Operation {
                Message = text
                Code = Constants.ErrorCode.REQUEST_DELETED |> Custom |> Some
            }
        | _ -> Ok page)

let private parseHttpResponse page =
    Html.load page
    |> Result.bind Common.pageHasError
    |> Result.bind pageHasInconsistentState
    |> Result.bind (Html.getNodes "//input")
    |> Result.bind (function
        | None -> Error <| NotFound "Kdmid data on the 'Validation Page' not found."
        | Some nodes ->
            nodes
            |> Seq.choose (fun node ->
                match node |> Html.getAttributeValue "name", node |> Html.getAttributeValue "value" with
                | Ok(Some name), Ok(Some value) -> Some(name, value)
                | Ok(Some name), Ok(None) -> Some(name, String.Empty)
                | _ -> None)
            |> Map.ofSeq
            |> Ok)
    |> Result.bind (fun result ->
        let requiredKeys = Set [ "__VIEWSTATE"; "__EVENTVALIDATION" ]

        let notRequiredKeys = Set [ "__VIEWSTATEGENERATOR" ]

        let requiredResult = result |> Map.filter (fun key _ -> requiredKeys.Contains key)

        let notRequiredResult =
            result |> Map.filter (fun key _ -> notRequiredKeys.Contains key)

        match requiredKeys.Count = requiredResult.Count with
        | true -> Ok(requiredResult |> Map.combine <| notRequiredResult)
        | false -> Error <| NotFound "Kdmid headers of the 'Validation Page headers' not found.")

let private prepareHttpFormData data =
    data
    |> Map.add "ctl00$MainContent$ButtonB.x" "100"
    |> Map.add "ctl00$MainContent$ButtonB.y" "20"
    |> Map.add "ctl00$MainContent$FeedbackClientID" "0"
    |> Map.add "ctl00$MainContent$FeedbackOrderID" "0"

let parse queryParams formData =
    fun (httpClient, postValidationPage) ->

        // define
        let postRequest = formData |> createHttpRequest queryParams ||> postValidationPage
        let parseResponse = ResultAsync.bind parseHttpResponse
        let prepareFormData = ResultAsync.mapAsync prepareHttpFormData

        // pipe
        httpClient |> postRequest |> parseResponse |> prepareFormData
