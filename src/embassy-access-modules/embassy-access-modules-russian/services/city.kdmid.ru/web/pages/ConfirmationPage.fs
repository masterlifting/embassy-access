﻿module internal EA.Embassies.Russian.Kdmid.Web.ConfirmationPage

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Parser
open Web.Clients.Domain.Http
open EA.Core.Domain
open EA.Embassies.Russian.Kdmid.Web
open EA.Embassies.Russian.Kdmid.Dependencies

let private handleRequestConfirmation (request: Request) =
    match request.ConfirmationState with
    | ConfirmationState.Disabled -> Ok <| None
    | ConfirmationState.Manual appointmentId ->
        match request.Appointments |> Seq.tryFind (fun x -> x.Id = appointmentId) with
        | Some appointment -> Ok <| Some appointment
        | None -> Error <| NotFound $"AppointmentId '{appointmentId.ValueStr}' not found."
    | ConfirmationState.Auto confirmationOption ->
        match request.Appointments.Count > 0, confirmationOption with
        | false, _ -> Ok None
        | true, FirstAvailable ->
            match request.Appointments |> Seq.tryHead with
            | Some appointment -> Ok <| Some appointment
            | None -> Error <| NotFound "First available appointment not found."
        | true, LastAvailable ->
            match request.Appointments |> Seq.tryLast with
            | Some appointment -> Ok <| Some appointment
            | None -> Error <| NotFound "Last available appointment not found."
        | true, DateTimeRange(min, max) ->

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
                <| NotFound
                    $"Appointment in the range '{min.ToShortDateString()}' - '{max.ToShortDateString()}' not found."

let private createHttpRequest formData queryParamsId =

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
    |> Result.bind Html.pageHasError
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

let private handlePage (deps: Order.Dependencies, httpClient, queryParamsId, formData, request) =
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
            let setConfirmation = ResultAsync.map (setConfirmation request appointment)

            // pipe
            httpClient
            |> postRequest
            |> parseResponse
            |> parseConfirmation
            |> setConfirmation
        | None -> request |> Ok |> async.Return)

let handle deps =
    ResultAsync.bindAsync (fun (httpClient, queryParamsId, formData, request) ->
        handlePage (deps, httpClient, queryParamsId, formData, request))
