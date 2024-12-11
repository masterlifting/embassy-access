module internal EA.Embassies.Russian.Kdmid.Web.AppointmentsPage

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Parser
open Web.Http.Domain
open EA.Core.Domain
open EA.Embassies.Russian.Kdmid.Web
open EA.Embassies.Russian.Kdmid.Html
open EA.Embassies.Russian.Kdmid.Domain

let private createHttpRequest formData queryParams =

    let request =
        { Path = $"/queue/orderinfo.aspx?%s{queryParams}"
          Headers = None }

    let content: RequestContent =
        String
            {| Data = formData
               Encoding = Text.Encoding.ASCII
               MediaType = "application/x-www-form-urlencoded" |}

    request, content

let private parseHttpResponse page =
    Html.load page
    |> Result.bind pageHasError
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

let private createRequestAppointments (formData: Map<string, string>, data) =

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
                <| { Id = AppointmentId.New
                     Value = value
                     Date = date
                     Time = time
                     Confirmation = None
                     Description = window }

            | _ -> Error <| NotSupported $"Appointment date: %s{dateTime}."
        | _ -> Error <| NotSupported $"Appointment row: %s{value}."

    match appointments.IsEmpty with
    | true -> Ok(formData, Set.empty)
    | false ->
        appointments
        |> Set.map parse
        |> Result.choose
        |> Result.map Set.ofList
        |> Result.map (fun appointments -> formData, appointments)

let private createResult (request: Request) (formData, appointments) =
    let request =
        { request with
            Appointments = appointments }

    formData, request

let private handlePage (deps, httpClient, queryParams, formData, request) =

    // define
    let postRequest =
        let formData = Http.buildFormData formData
        let request, content = createHttpRequest formData queryParams
        deps.postAppointmentsPage request content

    let parseResponse = ResultAsync.bind parseHttpResponse
    let prepareFormData = ResultAsync.mapAsync prepareHttpFormData
    let parseAppointments = ResultAsync.bind createRequestAppointments
    let createResult = ResultAsync.map (createResult request)

    // pipe
    httpClient
    |> postRequest
    |> parseResponse
    |> prepareFormData
    |> parseAppointments
    |> createResult

let handle deps =
    ResultAsync.bindAsync (fun (httpClient, queryParams, formData, request) ->
        queryParams
        |> Http.getQueryParamsId
        |> ResultAsync.wrap (fun queryParamsId ->
            handlePage (deps, httpClient, queryParams, formData, request)
            |> ResultAsync.map (fun (formData, request) -> httpClient, queryParamsId, formData, request)))
