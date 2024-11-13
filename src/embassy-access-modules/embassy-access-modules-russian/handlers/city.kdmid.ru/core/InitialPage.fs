module internal EA.Embassies.Russian.Kdmid.InitialPage

open System
open Infrastructure
open Infrastructure.Parser
open EA.Embassies.Russian.Kdmid.Web
open EA.Embassies.Russian.Kdmid.Common
open EA.Embassies.Russian.Kdmid.Domain

open SkiaSharp

type private Deps =
    { HttpClient: Web.Http.Domain.Client
      getInitialPage: HttpGetStringRequest
      getCaptcha: HttpGetBytesRequest
      solveCaptcha: SolveCaptchaImage }

let private createDeps (deps: ProcessRequestDeps) httpClient =
    { HttpClient = httpClient
      getInitialPage = deps.getInitialPage
      getCaptcha = deps.getCaptcha
      solveCaptcha = deps.solveCaptcha }

let private createHttpRequest queryParams =
    { Web.Http.Domain.Request.Path = "/queue/orderinfo.aspx?" + queryParams
      Web.Http.Domain.Request.Headers = None }

let private parseHttpResponse page =
    Html.load page
    |> Result.bind pageHasError
    |> Result.bind (Html.getNodes "//input | //img")
    |> Result.bind (function
        | None -> Error <| NotFound "Nodes on the Initial Page."
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
            Set
                [ "captchaUrlPath"
                  "__VIEWSTATE"
                  "__EVENTVALIDATION"
                  "ctl00$MainContent$txtID"
                  "ctl00$MainContent$txtUniqueID"
                  "ctl00$MainContent$ButtonA" ]

        let notRequiredKeys = Set [ "__VIEWSTATEGENERATOR" ]

        let requiredResult = result |> Map.filter (fun key _ -> requiredKeys.Contains key)

        let notRequiredResult =
            result |> Map.filter (fun key _ -> notRequiredKeys.Contains key)

        match requiredKeys.Count = requiredResult.Count with
        | true -> Ok(requiredResult |> Map.combine <| notRequiredResult)
        | false -> Error <| NotFound "Initial Page headers.")

let private createCaptchaRequest urlPath =
    { Web.Http.Domain.Request.Path = $"/queue/%s{urlPath}"
      Web.Http.Domain.Request.Headers = None }

let private prepareCaptchaImage (image: byte array) =
    try
        if image.Length = 0 then
            Error <| NotFound "Captcha image is empty."
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
        Error <| NotSupported ex.Message

let private prepareHttpFormData pageData captcha =
    pageData
    |> Map.remove "captchaUrlPath"
    |> Map.add "ctl00$MainContent$txtCode" $"%i{captcha}"
    |> Map.add "ctl00$MainContent$FeedbackClientID" "0"
    |> Map.add "ctl00$MainContent$FeedbackOrderID" "0"

let private handlePage (deps, queryParams) =

    // define
    let getRequest =
        let request = createHttpRequest queryParams
        deps.getInitialPage request

    let setCookie = ResultAsync.bind (deps.HttpClient |> Http.setRequiredCookie)
    let parseResponse = ResultAsync.bind parseHttpResponse

    // pipe
    deps.HttpClient
    |> getRequest
    |> setCookie
    |> parseResponse
    |> ResultAsync.bindAsync (fun pageData ->
        match pageData |> Map.tryFind "captchaUrlPath" with
        | None -> async { return Error <| NotFound "Captcha information on the Initial Page." }
        | Some urlPath ->

            // define
            let getCaptchaRequest =
                let request = createCaptchaRequest urlPath
                deps.getCaptcha request

            let setCookie = ResultAsync.bind (deps.HttpClient |> Http.setSessionCookie)
            let prepareResponse = ResultAsync.bind prepareCaptchaImage
            let solveCaptcha = ResultAsync.bindAsync deps.solveCaptcha
            let prepareFormData = ResultAsync.mapAsync (pageData |> prepareHttpFormData)
            let buildFormData = ResultAsync.mapAsync Http.buildFormData

            // pipe
            deps.HttpClient
            |> getCaptchaRequest
            |> setCookie
            |> prepareResponse
            |> solveCaptcha
            |> prepareFormData
            |> buildFormData)

let handle deps =
    ResultAsync.bindAsync (fun (httpClient, credentials: Credentials, request) ->
        let deps = createDeps deps httpClient
        let _, id, cd, ems = credentials.Value
        let queryParams = Http.createQueryParams id cd ems

        handlePage (deps, queryParams)
        |> ResultAsync.map (fun formData -> httpClient, queryParams, formData, request))
