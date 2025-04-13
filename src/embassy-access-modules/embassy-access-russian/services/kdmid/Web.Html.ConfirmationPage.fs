module EA.Russian.Services.Kdmid.Web.Html.ConfirmationPage

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Parser
open Web.Clients.Domain.Http
open EA.Core.Domain
open EA.Russian.Services.Kdmid.Web

let private handleRequestConfirmation (request: Request) =
    match request.ConfirmationState with
    | ConfirmationState.Disabled -> Ok <| None
    | ConfirmationState.Appointment appointmentId ->
        match request.Appointments |> Seq.tryFind (fun x -> x.Id = appointmentId) with
        | Some appointment -> Ok <| Some appointment
        | None -> Error <| NotFound $"AppointmentId '{appointmentId.ValueStr}' not found."
    | ConfirmationState.FirstAvailable ->
        match request.Appointments |> Seq.tryHead with
        | Some appointment -> Ok <| Some appointment
        | None -> Error <| NotFound "First available appointment not found."
    | ConfirmationState.LastAvailable ->
        match request.Appointments |> Seq.tryLast with
        | Some appointment -> Ok <| Some appointment
        | None -> Error <| NotFound "Last available appointment not found."
    | ConfirmationState.DateTimeRange(min, max) ->

        let minDate = DateOnly.FromDateTime min
        let maxDate = DateOnly.FromDateTime max

        let minTime = TimeOnly.FromDateTime min
        let maxTime = TimeOnly.FromDateTime max

        let appointment =
            request.Appointments
            |> Seq.filter (fun x -> x.Date >= minDate && x.Date <= maxDate)
            |> Seq.filter (fun x -> x.Time >= minTime && x.Time <= maxTime)
            |> Seq.tryHead

        match appointment with
        | Some appointment -> Ok <| Some appointment
        | None ->
            Error
            <| NotFound $"Appointment in the range '{min.ToShortDateString()}' - '{max.ToShortDateString()}' not found."

let private createHttpRequest queryParamsId formData =

    let request = {
        Path = $"/queue/SPCalendar.aspx?bjo=%s{queryParamsId}"
        Headers = None
    }

    let content: RequestContent =
        String {|
            Data = formData
            Encoding = Text.Encoding.ASCII
            MediaType = "application/x-www-form-urlencoded"
        |}

    request, content

let private parseHttpResponse page =
    Html.load page
    |> Result.bind Common.pageHasError
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

let private setConfirmation request (appointment: Appointment) (confirmation: Confirmation) =
    let appointment = {
        appointment with
            Confirmation = Some confirmation
    }

    let appointments =
        request.Appointments
        |> Set.filter (fun x -> x.Id <> appointment.Id)
        |> Set.add appointment

    {
        request with
            Appointments = appointments
            ConfirmationState = Disabled
    }

let parse queryParams formDataMap request =
    fun (httpClient, postConfirmationPage) ->
        request
        |> handleRequestConfirmation
        |> ResultAsync.wrap (function
            | Some appointment ->
                // define
                let postRequest queryParamsId =
                    appointment.Value
                    |> prepareHttpFormData formDataMap
                    |> Http.buildFormData
                    |> createHttpRequest queryParamsId
                    ||> postConfirmationPage

                let parseResponse = ResultAsync.bind parseHttpResponse
                let parseConfirmation = ResultAsync.bind createRequestConfirmation
                let setConfirmation = ResultAsync.map (setConfirmation request appointment)

                // pipe
                queryParams
                |> Http.getQueryParamsId
                |> ResultAsync.wrap (fun queryParamsId ->
                    httpClient
                    |> postRequest queryParamsId
                    |> parseResponse
                    |> parseConfirmation
                    |> setConfirmation)
            | None -> request |> Ok |> async.Return)
