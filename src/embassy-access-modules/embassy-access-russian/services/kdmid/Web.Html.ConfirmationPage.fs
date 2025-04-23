module EA.Russian.Services.Kdmid.Web.Html.ConfirmationPage

open System
open EA.Russian.Services.Domain.Kdmid
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Parser
open Web.Clients.Domain.Http
open EA.Core.Domain
open EA.Russian.Services.Kdmid.Web

let private handleRequestConfirmation (request: Request<Payload>) =
    match request.Payload.State with
    | NoAppointments -> Error <| NotFound "No appointments found to confirm."
    | HasConfirmation _ -> Error <| NotFound "Request already has confirmation."
    | HasAppointments appointments ->
        match request.Payload.Confirmation with
        | Confirmation.Disabled -> Ok <| None
        | Confirmation.ForAppointment appointmentId ->
            match appointments |> Seq.tryFind (fun x -> x.Id = appointmentId) with
            | Some appointment -> Ok <| Some appointment
            | None -> Error <| NotFound $"AppointmentId '{appointmentId.ValueStr}' not found."
        | Confirmation.FirstAvailable ->
            match appointments |> Seq.tryHead with
            | Some appointment -> Ok <| Some appointment
            | None -> Error <| NotFound "First available appointment not found."
        | Confirmation.LastAvailable ->
            match appointments |> Seq.tryLast with
            | Some appointment -> Ok <| Some appointment
            | None -> Error <| NotFound "Last available appointment not found."
        | Confirmation.DateTimeRange(min, max) ->

            let minDate = DateOnly.FromDateTime min
            let maxDate = DateOnly.FromDateTime max

            let minTime = TimeOnly.FromDateTime min
            let maxTime = TimeOnly.FromDateTime max

            let appointment =
                appointments
                |> Seq.filter (fun x -> x.Date >= minDate && x.Date <= maxDate)
                |> Seq.filter (fun x -> x.Time >= minTime && x.Time <= maxTime)
                |> Seq.tryHead

            match appointment with
            | Some appointment -> Ok <| Some appointment
            | None ->
                Error
                <| NotFound
                    $"Appointment in the range '{min.ToShortDateString()}' - '{max.ToShortDateString()}' not found."

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
    |> Result.bind (function
        | None -> "Confirmation data not found." |> NotFound |> Error
        | Some node ->
            match node.InnerText with
            | AP.IsString text -> Ok text
            | _ -> "Confirmation data not found." |> NotFound |> Error)

let private prepareHttpFormData data value =
    data
    |> Map.add "ctl00$MainContent$RadioButtonList1" value
    |> Map.add "ctl00$MainContent$TextBox1" value

let private setConfirmation (request: Request<Payload>) (appointment: Appointment) (confirmation: string) = {
    request with
        Payload = {
            request.Payload with
                Confirmation = Disabled
                State = HasConfirmation(confirmation, appointment)
        }
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
                let setConfirmation = ResultAsync.map (setConfirmation request appointment)

                // pipe
                queryParams
                |> Http.getQueryParamsId
                |> ResultAsync.wrap (fun queryParamsId ->
                    httpClient |> postRequest queryParamsId |> parseResponse |> setConfirmation)
            | None -> request |> Ok |> async.Return)
