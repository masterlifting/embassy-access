module internal EmbassyAccess.Embassies.Russian.Core

open System
open Infrastructure
open Infrastructure.Parser
open EmbassyAccess.Domain
open EmbassyAccess.Embassies.Russian.Domain

module private Http =
    open Web.Http.Domain
    open Web.Http.Client

    let private createClient' city =

        let host = $"%s{city}.kdmid.ru"
        let baseUrl = $"https://%s{host}"

        let headers =
            Map
                [ "Host", [ host ]
                  "Origin", [ baseUrl ]
                  "Accept",
                  [ "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/png,image/svg+xml,*/*;q=0.8" ]
                  "Accept-Language", [ "en-US,en;q=0.9,ru;q=0.8" ]
                  "Connection", [ "keep-alive" ]
                  "Sec-Fetch-Dest", [ "document" ]
                  "Sec-Fetch-Mode", [ "navigate" ]
                  "Sec-Fetch-Site", [ "same-origin" ]
                  "Sec-Fetch-User", [ "?1" ]
                  "Upgrade-Insecure-Requests", [ "1" ]
                  "User-Agent", [ "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:129.0) Gecko/20100101 Firefox/129.0" ] ]
            |> Some

        create baseUrl headers

    let createClient =
        ResultAsync.bind (fun (credentials: Credentials, request) ->
            let city, _, _, _ = credentials.Value

            city
            |> createClient'
            |> Result.map (fun httpClient -> httpClient, credentials, request))

    let createQueryParams id cd ems =
        match ems with
        | Some ems -> $"id=%i{id}&cd=%s{cd}&ems=%s{ems}"
        | None -> $"id=%i{id}&cd=%s{cd}"

    let getQueryParamsId queryParams =
        queryParams
        |> Route.fromQueryParams
        |> Result.map (Map.tryFind "id")
        |> Result.bind (function
            | Some id -> Ok id
            | None -> Error <| NotFound "Query parameter 'id'.")

    let private setCookie cookie httpClient =
        let headers = Map [ "Cookie", cookie ] |> Some
        httpClient |> Headers.set headers

    let setRequiredCookie httpClient (response: Response<string>) =
        response.Headers
        |> Headers.tryFind "Set-Cookie" [ "AlteonP"; "__ddg1_" ]
        |> Option.map (fun cookie -> httpClient |> setCookie cookie |> Result.map (fun _ -> response.Content))
        |> Option.defaultValue (Ok response.Content)

    let setSessionCookie httpClient (response: Response<byte array>) =
        response.Headers
        |> Headers.tryFind "Set-Cookie" [ "ASP.NET_SessionId" ]
        |> Option.map (fun cookie -> httpClient |> setCookie cookie |> Result.map (fun _ -> response.Content))
        |> Option.defaultValue (Ok response.Content)

    let buildFormData data =
        data
        |> Map.add "__EVENTTARGET" ""
        |> Map.add "__EVENTARGUMENT" ""
        |> Seq.map (fun x -> $"{Uri.EscapeDataString x.Key}={Uri.EscapeDataString x.Value}")
        |> String.concat "&"

let private hasError page =
    page
    |> Html.getNode "//span[@id='ctl00_MainContent_lblCodeErr']"
    |> Result.bind (function
        | None -> Ok page
        | Some node ->
            match node.InnerText with
            | AP.IsString text ->
                Error
                <| Operation
                    { Message = text
                      Code = Some ErrorCodes.PageHasError }
            | _ -> Ok page)

module private InitialPage =
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
        |> Result.bind hasError
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

    let private handle' (deps, queryParams) =

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
        |> ResultAsync.bind' (fun pageData ->
            match pageData |> Map.tryFind "captchaUrlPath" with
            | None -> async { return Error <| NotFound "Captcha information on the Initial Page." }
            | Some urlPath ->

                // define
                let getCaptchaRequest =
                    let request = createCaptchaRequest urlPath
                    deps.getCaptcha request

                let setCookie = ResultAsync.bind (deps.HttpClient |> Http.setSessionCookie)
                let prepareResponse = ResultAsync.bind prepareCaptchaImage
                let solveCaptcha = ResultAsync.bind' deps.solveCaptcha
                let prepareFormData = ResultAsync.map' (pageData |> prepareHttpFormData)
                let buildFormData = ResultAsync.map' Http.buildFormData

                // pipe
                deps.HttpClient
                |> getCaptchaRequest
                |> setCookie
                |> prepareResponse
                |> solveCaptcha
                |> prepareFormData
                |> buildFormData)

    let handle deps =
        ResultAsync.bind' (fun (httpClient, credentials: Credentials, request) ->
            let deps = createDeps deps httpClient
            let _, id, cd, ems = credentials.Value
            let queryParams = Http.createQueryParams id cd ems

            handle' (deps, queryParams)
            |> ResultAsync.map (fun formData -> httpClient, queryParams, formData, request))

module private ValidationPage =
    open System.Text.RegularExpressions

    type private Deps =
        { HttpClient: Web.Http.Domain.Client
          postValidationPage: HttpPostStringRequest }

    let private createDeps (deps: ProcessRequestDeps) httpClient =
        { HttpClient = httpClient
          postValidationPage = deps.postValidationPage }

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
                              Code = Some ErrorCodes.ConfirmationExists }
                    | text when text |> has "Ваша заявка требует подтверждения" ->
                        Error
                        <| Operation
                            { Message = text
                              Code = Some ErrorCodes.NotConfirmed }
                    | text when text |> has "Заявка удалена" ->
                        Error
                        <| Operation
                            { Message = text
                              Code = Some ErrorCodes.RequestDeleted }
                    | _ -> Ok page
                | _ -> Ok page)

    let private parseHttpResponse page =
        Html.load page
        |> Result.bind hasError
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

    let private handle' (deps, queryParams, formData) =

        // define
        let postRequest =
            let request, content = createHttpRequest formData queryParams
            deps.postValidationPage request content

        let parseResponse = ResultAsync.bind parseHttpResponse
        let prepareFormData = ResultAsync.map' prepareHttpFormData

        // pipe
        deps.HttpClient |> postRequest |> parseResponse |> prepareFormData

    let handle deps =
        ResultAsync.bind' (fun (httpClient, queryParams, formData, request) ->
            let deps = createDeps deps httpClient

            handle' (deps, queryParams, formData)
            |> ResultAsync.map (fun formData -> httpClient, queryParams, formData, request))

module private AppointmentsPage =
    type private Deps =
        { HttpClient: Web.Http.Domain.Client
          postAppointmentsPage: HttpPostStringRequest }

    let private createDeps (deps: ProcessRequestDeps) httpClient =
        { HttpClient = httpClient
          postAppointmentsPage = deps.postAppointmentsPage }

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

    let private parseHttpResponse page =
        Html.load page
        |> Result.bind hasError
        |> Result.bind (Html.getNodes "//input")
        |> Result.bind (function
            | None -> Ok Map.empty
            | Some nodes ->
                nodes
                |> Seq.choose (fun node ->
                    match node |> Html.getAttributeValue "name", node |> Html.getAttributeValue "value" with
                    | Ok(Some name), Ok(Some value) -> Some(value, name)
                    | _ -> None)
                |> Map.ofSeq
                |> Ok)
        |> Result.map Map.reverse
        |> Result.bind (fun result ->
            let requiredKeys =
                Set [ "__VIEWSTATE"; "__EVENTVALIDATION"; "ctl00$MainContent$Button1" ]

            let notRequiredKeys =
                Set [ "__VIEWSTATEGENERATOR"; "ctl00$MainContent$RadioButtonList1" ]

            let requiredResult = result |> Map.filter (fun key _ -> requiredKeys.Contains key)

            let notRequiredResult =
                result |> Map.filter (fun key _ -> notRequiredKeys.Contains key)

            match requiredKeys.Count = requiredResult.Count with
            | true ->
                match
                    requiredResult
                    |> Map.forall (fun _ value -> value |> Seq.tryHead |> Option.isSome)
                with
                | true -> Ok(requiredResult |> Map.combine <| notRequiredResult)
                | false -> Error <| NotFound "AppointmentsPage Page headers."
            | false -> Error <| NotFound "AppointmentsPage Page headers.")

    let private prepareHttpFormData data =
        let requiredKeys =
            Set
                [ "__VIEWSTATE"
                  "__EVENTVALIDATION"
                  "__VIEWSTATEGENERATOR"
                  "ctl00$MainContent$Button1" ]

        let formData =
            data
            |> Map.filter (fun key _ -> requiredKeys.Contains key)
            |> Map.map (fun _ value -> value |> Seq.head)

        (formData, data)

    let private creatrRequestAppointments (formData: Map<string, string>, data) =

        let appointments =
            data
            |> Map.filter (fun key _ -> key = "ctl00$MainContent$RadioButtonList1")
            |> Map.values
            |> Seq.concat
            |> Set.ofSeq

        let parse (value: string) =
            let parts = value.Split '|'

            match parts.Length with
            | 4 ->
                let dateTime = parts[1]
                let window = parts[3]

                let date = DateOnly.TryParse dateTime
                let time = TimeOnly.TryParse dateTime

                match date, time with
                | (true, date), (true, time) ->
                    Ok
                    <| { Value = value
                         Date = date
                         Time = time
                         Confirmation = None
                         Description = Some window }

                | _ -> Error <| NotSupported $"Appointment date: %s{dateTime}."
            | _ -> Error <| NotSupported $"Appointment row: %s{value}."

        match appointments.IsEmpty with
        | true -> Ok(formData, Set.empty)
        | false ->
            appointments
            |> Set.map parse
            |> Seq.roe
            |> Result.map Set.ofList
            |> Result.map (fun appointments -> formData, appointments)

    let private createResult request (formData, appointments) =
        let request =
            { request with
                Appointments = appointments }

        formData, request

    let private handle' (deps, queryParams, formData, request) =

        // define
        let postRequest =
            let formData = Http.buildFormData formData
            let request, content = createHttpRequest formData queryParams
            deps.postAppointmentsPage request content

        let parseResponse = ResultAsync.bind parseHttpResponse
        let prepareFormData = ResultAsync.map' prepareHttpFormData
        let parseAppointments = ResultAsync.bind creatrRequestAppointments
        let createResult = ResultAsync.map (createResult request)

        // pipe
        deps.HttpClient
        |> postRequest
        |> parseResponse
        |> prepareFormData
        |> parseAppointments
        |> createResult

    let handle deps =
        ResultAsync.bind' (fun (httpClient, queryParams, formData, request) ->
            let deps = createDeps deps httpClient

            queryParams
            |> Http.getQueryParamsId
            |> ResultAsync.wrap (fun queryParamsId ->
                handle' (deps, queryParams, formData, request)
                |> ResultAsync.map (fun (formData, request) -> httpClient, queryParamsId, formData, request)))

module private ConfirmationPage =

    type private Deps =
        { HttpClient: Web.Http.Domain.Client
          postConfirmationPage: HttpPostStringRequest }

    let private createDeps (deps: ProcessRequestDeps) httpClient =
        { HttpClient = httpClient
          postConfirmationPage = deps.postConfirmationPage }

    let private handleRequestConfirmation request =
        match request.ConfirmationState with
        | Disabled -> Ok <| None
        | Manual appointment ->
            match request.Appointments |> Seq.tryFind (fun x -> x.Value = appointment.Value) with
            | Some appointment -> Ok <| Some appointment
            | None -> Error <| NotFound $"Appointment '%s{appointment.Value}'."
        | Auto confirmationOption ->
            match request.Appointments.Count > 0, confirmationOption with
            | false, _ -> Ok None
            | true, FirstAvailable ->
                match request.Appointments |> Seq.tryHead with
                | Some appointment -> Ok <| Some appointment
                | None -> Error <| NotFound "First available appointment."
            | true, LastAvailable ->
                match request.Appointments |> Seq.tryLast with
                | Some appointment -> Ok <| Some appointment
                | None -> Error <| NotFound "Last available appointment."
            | true, DateTimeRange(min, max) ->

                let minDate = DateOnly.FromDateTime(min)
                let maxDate = DateOnly.FromDateTime(max)

                let minTime = TimeOnly.FromDateTime(min)
                let maxTime = TimeOnly.FromDateTime(max)

                let appointment =
                    request.Appointments
                    |> Seq.filter (fun x -> x.Date >= minDate && x.Date <= maxDate)
                    |> Seq.filter (fun x -> x.Time >= minTime && x.Time <= maxTime)
                    |> Seq.tryHead

                match appointment with
                | Some appointment -> Ok <| Some appointment
                | None ->
                    Error
                    <| NotFound $"Appointment in range '{min.ToShortDateString()}' - '{max.ToShortDateString()}'."

    let private createHttpRequest formData queryParamsId =

        let request =
            { Web.Http.Domain.Request.Path = $"/queue/SPCalendar.aspx?bjo=%s{queryParamsId}"
              Web.Http.Domain.Request.Headers = None }

        let content: Web.Http.Domain.RequestContent =
            Web.Http.Domain.String
                {| Data = formData
                   Encoding = Text.Encoding.ASCII
                   MediaType = "application/x-www-form-urlencoded" |}

        request, content

    let private parseHttpResponse page =
        Html.load page
        |> Result.bind hasError
        |> Result.bind (Html.getNode "//span[@id='ctl00_MainContent_Label_Message']")
        |> Result.map (function
            | None -> None
            | Some node ->
                match node.InnerText with
                | AP.IsString text -> Some text
                | _ -> None)

    let private prepareHttpFormData data value =
        data
        |> Map.add "ctl00$MainContent$RadioButtonList1" value
        |> Map.add "ctl00$MainContent$TextBox1" value

    let private createRequestConfirmation =
        function
        | None -> Error <| NotFound "Confirmation data."
        | Some data -> Ok { Description = data }

    let private createResult request appointment confirmation =
        let appointment =
            { appointment with
                Confirmation = Some confirmation }

        let appointments =
            request.Appointments
            |> Set.filter (fun x -> x.Value <> appointment.Value)
            |> Set.add appointment

        { request with
            Appointments = appointments
            ConfirmationState = Disabled }

    let private createDefaultResult request =
        async {
            return
                Ok
                <| match request.ConfirmationState with
                   | Manual _ ->
                       { request with
                           ConfirmationState = Disabled }
                   | _ -> request
        }

    let private handle' (deps, queryParamsId, formData, request) =
        request
        |> handleRequestConfirmation
        |> ResultAsync.wrap (function
            | Some appointment ->
                // define
                let postRequest =
                    let formData =
                        appointment.Value |> prepareHttpFormData formData |> Http.buildFormData

                    let request, content = createHttpRequest formData queryParamsId
                    deps.postConfirmationPage request content

                let parseResponse = ResultAsync.bind parseHttpResponse
                let parseConfirmation = ResultAsync.bind createRequestConfirmation
                let createResult = ResultAsync.map (createResult request appointment)

                // pipe
                deps.HttpClient
                |> postRequest
                |> parseResponse
                |> parseConfirmation
                |> createResult
            | None -> request |> createDefaultResult)

    let handle deps =
        ResultAsync.bind' (fun (httpClient, queryParamsId, formData, request) ->
            let deps = createDeps deps httpClient
            handle' (deps, queryParamsId, formData, request))

module private Request =

    let private validateCredentials request credentials =
        match request.Embassy.Country.City = credentials.City with
        | true -> Ok credentials
        | false ->
            Error
            <| NotSupported
                $"Embassy city '%A{request.Embassy.Country.City}' is not matched with the requested City '%A{credentials.City}'."

    let createCredentials =
        ResultAsync.bind (fun request ->
            request.Payload
            |> createCredentials
            |> Result.bind (validateCredentials request)
            |> Result.map (fun credentials -> credentials, request))

    let setInProcessState deps request =
        deps.updateRequest
            { request with
                State = InProcess
                Description = None
                Modified = DateTime.UtcNow }

    let private setAttempt' timeShift request =
        let timeShift = timeShift |> int
        let modifiedDay = request.Modified.AddHours timeShift
        let today = DateTime.UtcNow.AddHours timeShift

        match modifiedDay.DayOfYear = today.DayOfYear, request.Attempt > 20 with
        | true, true ->
            Error
            <| Cancelled $"The request was cancelled due to the number of attempts reached the %i{request.Attempt}."
        | false, true ->
            Ok
            <| { request with
                   Attempt = 1
                   Modified = DateTime.UtcNow }
        | _ ->
            Ok
            <| { request with
                   Attempt = request.Attempt + 1
                   Modified = DateTime.UtcNow }

    let setAttempt deps =
        ResultAsync.bind' (fun (httpClient, queryParams, formData, request) ->
            request
            |> setAttempt' deps.Configuration.TimeShift
            |> ResultAsync.wrap deps.updateRequest
            |> ResultAsync.map (fun request -> httpClient, queryParams, formData, request))

    let private setCompletedState deps request =
        let message =
            match request.Appointments.IsEmpty with
            | true -> "No appointments found"
            | false ->
                match request.Appointments |> Seq.choose (fun x -> x.Confirmation) |> List.ofSeq with
                | [] -> $"Found appointments: %i{request.Appointments.Count}"
                | confirmations -> $"Found confirmations: %i{confirmations.Length}"
            |> fun msg -> $"%s{msg}. Request: %s{request.Payload}"

        deps.updateRequest
            { request with
                State = Completed message
                Modified = DateTime.UtcNow }

    let private setFailedState error deps request =
        let attempt =
            match error with
            | Operation { Code = Some Web.Captcha.CaptchaErrorCode } -> request.Attempt
            | _ -> request.Attempt + 1

        deps.updateRequest
            { request with
                State = Failed error
                Attempt = attempt
                Modified = DateTime.UtcNow }
        |> ResultAsync.bind (fun _ -> Error <| error.extendMessage $"Request: %s{request.Payload}")

    let completeConfirmation deps request confirmation =
        async {
            match! confirmation with
            | Error error -> return! request |> setFailedState error deps
            | Ok request -> return! request |> setCompletedState deps
        }

let processRequest deps request =

    // define
    let setRequestInProcessState = Request.setInProcessState deps
    let createRequestCredentials = Request.createCredentials
    let createHttpClient = Http.createClient
    let processInitialPage = InitialPage.handle deps
    let setRequestAttempt = Request.setAttempt deps
    let processValidationPage = ValidationPage.handle deps
    let processAppointmentsPage = AppointmentsPage.handle deps
    let processConfirmationPage = ConfirmationPage.handle deps
    let setRequestFinalState = Request.completeConfirmation deps request

    // pipe
    let start =
        setRequestInProcessState
        >> createRequestCredentials
        >> createHttpClient
        >> processInitialPage
        >> setRequestAttempt
        >> processValidationPage
        >> processAppointmentsPage
        >> processConfirmationPage
        >> setRequestFinalState

    request |> start

let processRequestDeps ct config storage =
    { Configuration = config
      updateRequest =
        fun request ->
            storage
            |> EmbassyAccess.Persistence.Repository.Command.Request.update ct request
      getInitialPage =
        fun request client ->
            client
            |> Web.Http.Client.Request.get ct request
            |> Web.Http.Client.Response.String.read ct
      getCaptcha =
        fun request client ->
            client
            |> Web.Http.Client.Request.get ct request
            |> Web.Http.Client.Response.Bytes.read ct
      solveCaptcha = Web.Captcha.solveToInt ct
      postValidationPage =
        fun request content client ->
            client
            |> Web.Http.Client.Request.post ct request content
            |> Web.Http.Client.Response.String.readContent ct
      postAppointmentsPage =
        fun request content client ->
            client
            |> Web.Http.Client.Request.post ct request content
            |> Web.Http.Client.Response.String.readContent ct
      postConfirmationPage =
        fun request content client ->
            client
            |> Web.Http.Client.Request.post ct request content
            |> Web.Http.Client.Response.String.readContent ct }
