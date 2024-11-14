module internal EA.Embassies.Russian.Kdmid.ValidationPage

open System
open System.Text.RegularExpressions

open Infrastructure
open Infrastructure.Parser
open EA.Embassies.Russian.Kdmid.Html
open EA.Embassies.Russian.Kdmid.Domain

let private createHttpRequest formData queryParams =

    let request =
        { Web.Http.Domain.Request.Path = $"/queue/orderinfo.aspx?%s{queryParams}"
          Web.Http.Domain.Request.Headers = None }

    let content: Web.Http.Domain.RequestContent =
        Web.Http.Domain.String
            {| Data = formData
               Encoding = Text.Encoding.ASCII
               MediaType = "application/x-www-form-urlencoded" |}

    request, content

let private httpResponseHasInconsistentState page =
    page
    |> Html.getNode "//span[@id='ctl00_MainContent_Content'] | //span[@id='ctl00_MainContent_Label_Message']"
    |> Result.bind (function
        | None -> Ok page
        | Some node ->
            match node.InnerHtml with
            | AP.IsString text ->
                let text = Regex.Replace(text, @"<[^>]*>", Environment.NewLine)
                let text = Regex.Replace(text, @"\s+", " ")

                let has (pattern: string) (node: string) =
                    node.Contains(pattern, StringComparison.OrdinalIgnoreCase)

                match text with
                | text when text |> has "Вы записаны" && not (text |> has "список ожидания") ->
                    Error
                    <| Operation
                        { Message = text
                          Code = Some Constants.ErrorCodes.CONFIRMATIONS_EXISTS }
                | text when text |> has "Ваша заявка требует подтверждения" ->
                    Error
                    <| Operation
                        { Message = text
                          Code = Some Constants.ErrorCodes.NOT_CONFIRMED }
                | text when text |> has "Заявка удалена" ->
                    Error
                    <| Operation
                        { Message = text
                          Code = Some Constants.ErrorCodes.REQUEST_DELETED }
                | _ -> Ok page
            | _ -> Ok page)

let private parseHttpResponse page =
    Html.load page
    |> Result.bind pageHasError
    |> Result.bind httpResponseHasInconsistentState
    |> Result.bind (Html.getNodes "//input")
    |> Result.bind (function
        | None -> Error <| NotFound "Nodes on the Validation Page."
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
        | false -> Error <| NotFound "Validation Page headers.")

let private prepareHttpFormData data =
    data
    |> Map.add "ctl00$MainContent$ButtonB.x" "100"
    |> Map.add "ctl00$MainContent$ButtonB.y" "20"
    |> Map.add "ctl00$MainContent$FeedbackClientID" "0"
    |> Map.add "ctl00$MainContent$FeedbackOrderID" "0"

let private handlePage (deps, httpClient, queryParams, formData) =

    // define
    let postRequest =
        let request, content = createHttpRequest formData queryParams
        deps.postValidationPage request content

    let parseResponse = ResultAsync.bind parseHttpResponse
    let prepareFormData = ResultAsync.mapAsync prepareHttpFormData

    // pipe
    httpClient |> postRequest |> parseResponse |> prepareFormData

let handle deps =
    ResultAsync.bindAsync (fun (httpClient, queryParams, formData, request) ->
        handlePage (deps, httpClient, queryParams, formData)
        |> ResultAsync.map (fun formData -> httpClient, queryParams, formData, request))
