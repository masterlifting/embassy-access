module EA.Russian.Services.Kdmid.Web.Html.AppointmentsPage

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Http
open Web.Clients.Browser
open EA.Core.Domain
open EA.Russian.Services.Kdmid.Web
open EA.Russian.Services.Domain.Kdmid

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
        | text when text |> String.has "Ваша заявка заблокирована" ->
            Error
            <| Operation {
                Message = text
                Code = Constants.ErrorCode.REQUEST_BLOCKED |> Custom |> Some
            }
        | text when text |> String.has "Защитный код заявки задан неверно" ->
            Error
            <| Operation {
                Message = text
                Code = Constants.ErrorCode.REQUEST_NOT_FOUND |> Custom |> Some
            }
        | _ -> Ok page)

let private parseHttpResponse page =
    Html.load page
    |> Result.bind Common.pageHasError
    |> Result.bind pageHasInconsistentState
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
            | false -> Error <| NotFound "Kdmid 'Appointments Page' headers not found."
        | false -> Error <| NotFound "Kdmid 'Appointments Page' headers not found.")

let private prepareHttpFormData data =
    let requiredKeys =
        Set [
            "__VIEWSTATE"
            "__EVENTVALIDATION"
            "__VIEWSTATEGENERATOR"
            "ctl00$MainContent$Button1"
        ]

    let formData =
        data
        |> Map.filter (fun key _ -> requiredKeys.Contains key)
        |> Map.map (fun _ value -> value |> Seq.head)

    (formData, data)

let private createRequestAppointments (request: Request<Payload>) (formData: Map<string, string>, data) =

    let parse (value: string) =
        let parts = value.Split '|'

        match parts.Length with
        | 4 ->
            let dateTime = parts[1]
            let description = parts[3]

            let date = DateOnly.TryParse dateTime
            let time = TimeOnly.TryParse dateTime

            match date, time with
            | (true, date), (true, time) ->

                let appointmentId =
                    match request.Payload.State with
                    | NoAppointments -> AppointmentId.createNew ()
                    | HasAppointments appointments ->
                        appointments
                        |> Seq.tryFind (fun appointment -> appointment.Value = value)
                        |> Option.map _.Id
                        |> Option.defaultValue (AppointmentId.createNew ())
                    | HasConfirmation(_, appointment) ->
                        match appointment.Value = value with
                        | true -> appointment.Id
                        | false -> AppointmentId.createNew ()

                {
                    Id = appointmentId
                    Value = value
                    Date = date
                    Time = time
                    Description = description
                }
                |> Ok
            | _ ->
                Error
                <| NotSupported $"Kdmid 'Appointments page' date '%s{dateTime}' is not supported."
        | _ ->
            Error
            <| NotSupported $"Kdmid 'Appointments page' row '%s{value}' is not supported."

    let appointments =
        data
        |> Map.filter (fun key _ -> key = "ctl00$MainContent$RadioButtonList1")
        |> Map.values
        |> Seq.concat
        |> Set.ofSeq

    match appointments.IsEmpty with
    | true -> Ok(formData, Set.empty)
    | false ->
        appointments
        |> Set.map parse
        |> Result.choose
        |> Result.map Set.ofList
        |> Result.map (fun appointments -> formData, appointments)

let private createResult (request: Request<Payload>) (formData, appointments: Set<Appointment>) =
    formData,
    {
        request with
            Payload = {
                request.Payload with
                    State =
                        match appointments.IsEmpty with
                        | true -> NoAppointments
                        | false -> HasAppointments appointments
            }
    }

let parse queryParams formDataMap request =
    fun (httpClient, postAppointmentsPage) ->

        // define
        let postRequest =
            formDataMap
            |> Http.buildFormData
            |> createHttpRequest queryParams
            ||> postAppointmentsPage

        let parseResponse = ResultAsync.bind parseHttpResponse
        let prepareFormData = ResultAsync.mapAsync prepareHttpFormData
        let parseAppointments = ResultAsync.bind (createRequestAppointments request)
        let createResult = ResultAsync.map (createResult request)

        // pipe
        httpClient
        |> postRequest
        |> parseResponse
        |> prepareFormData
        |> parseAppointments
        |> createResult
