module EA.Russian.Clients.Kdmid.Web.Html.InitialPage

open System
open EA.Russian.Clients.Kdmid
open SkiaSharp
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Parser
open Web.Clients.Domain.Http
open EA.Russian.Clients.Kdmid.Web

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

let private prepareCaptchaImage (image: byte array) =
    try
        if image.Length = 0 then
            Error <| NotFound "Kdmid 'captcha image' not found."
        else
            let bitmap = image |> SKBitmap.Decode
            let bitmapInfo = bitmap.Info
            let bitmapPixels = bitmap.GetPixels()

            use pixmap = new SKPixmap(bitmapInfo, bitmapPixels)

            if pixmap.Height = pixmap.Width then
                Ok image
            else
                let x = pixmap.Width / 3
                let y = 0
                let width = x * 2
                let height = pixmap.Height

                let subset = pixmap.ExtractSubset <| SKRectI(x, y, width, height)
                let data = subset.Encode(SKEncodedImageFormat.Png, 100)

                Ok <| data.ToArray()
    with ex ->
        Error
        <| Operation {
            Message = "Kdmid 'captcha image' error. " + (ex |> Exception.toMessage)
            Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
        }

let private prepareHttpFormData pageData captcha =
    pageData
    |> Map.remove "captchaUrlPath"
    |> Map.add "ctl00$MainContent$txtCode" $"%i{captcha}"
    |> Map.add "ctl00$MainContent$FeedbackClientID" "0"
    |> Map.add "ctl00$MainContent$FeedbackOrderID" "0"

let parse queryParams =
    fun (httpClient, getInitialPage, getCaptcha, solveIntCaptcha) ->
        // define
        let getRequest = queryParams |> createHttpRequest |> getInitialPage
        let setCookie = ResultAsync.bind (httpClient |> Http.setRequiredCookie)
        let parseResponse = ResultAsync.bind parseHttpResponse

        // pipe
        httpClient
        |> getRequest
        |> setCookie
        |> parseResponse
        |> ResultAsync.bindAsync (fun pageData ->
            match pageData |> Map.tryFind "captchaUrlPath" with
            | None ->
                "Kdmid 'captcha' information on the 'Initial Page' not found."
                |> NotFound
                |> Error
                |> async.Return
            | Some urlPath ->

                // define
                let getCaptchaRequest = urlPath |> createCaptchaRequest |> getCaptcha
                let setCookie = ResultAsync.bind (httpClient |> Http.setSessionCookie)
                let prepareResponse = ResultAsync.bind prepareCaptchaImage
                let rec solveCaptcha attempts =
                    ResultAsync.bindAsync (fun data ->
                        async {
                            match! data |> solveIntCaptcha with
                            | Ok captcha -> return Ok captcha
                            | Error error ->
                                match attempts <= 0 with
                                | true -> return Error error
                                | false ->
                                    do! Async.Sleep 1000
                                    let data = data |> Ok |> async.Return
                                    return! data |> solveCaptcha (attempts - 1)
                        })

                let prepareFormData = ResultAsync.mapAsync (pageData |> prepareHttpFormData)
                let buildFormData = ResultAsync.mapAsync Http.buildFormData

                // pipe
                httpClient
                |> getCaptchaRequest
                |> setCookie
                |> prepareResponse
                |> (solveCaptcha 3)
                |> prepareFormData
                |> buildFormData)
