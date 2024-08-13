module EmbassyAccess.Embassies.Russian.Test

open System
open Expecto
open Infrastructure
open EmbassyAccess
open EmbassyAccess.Embassies.Russian.Domain

module private Fixture =
    open Web.Http.Domain
    open Persistence.FileSystem
    open EmbassyAccess.Domain

    let request: Request =
        { Id = RequestId.New
          Payload = "https://berlin.kdmid.ru/queue/orderinfo.aspx?id=290383&cd=B714253F"
          Embassy = Russian <| Germany Berlin
          State = Created
          Attempt = 0
          Confirmation = None
          Appointments = Set.empty
          Description = None
          Modified = DateTime.UtcNow }

    let requiredHeaders =
        Some
        <| Map [ "Set-Cookie", [ "ASP.NET_SessionId=1"; " AlteonP=1"; " __ddg1_=1" ] ]

    let httpHetStringRequest fileName =
        Environment.CurrentDirectory + $"/embassies/russian/test_data/{fileName}.html"
        |> Storage.create
        |> ResultAsync.wrap Storage.Read.string
        |> ResultAsync.map (fun data ->
            { Content = data
              Headers = requiredHeaders
              StatusCode = 200 })

    let httpPostStringRequest fileName =
        Environment.CurrentDirectory + $"/embassies/russian/test_data/{fileName}.html"
        |> Storage.create
        |> ResultAsync.wrap Storage.Read.string

    let httpGetBytesRequest fileName =
        Environment.CurrentDirectory + "/embassies/russian/test_data/" + fileName
        |> Storage.create
        |> ResultAsync.wrap Storage.Read.bytes
        |> ResultAsync.map (fun data ->
            { Content = data
              Headers = requiredHeaders
              StatusCode = 200 })

    let processRequestDeps =
        { Configuration = { TimeShift = 0y }
          updateRequest = fun _ -> async { return Ok() }
          getInitialPage = fun _ _ -> httpHetStringRequest "initial_page_response"
          getCaptcha = fun _ _ -> httpGetBytesRequest "captcha.png"
          solveCaptcha = fun _ -> async { return Ok 42 }
          postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_valid_response"
          postAppointmentsPage = fun _ _ _ -> httpPostStringRequest "appointments_page_has_result_1"
          postConfirmationPage = fun _ _ _ -> httpPostStringRequest "confirmation_page_valid_response" }

open Fixture

let private ``validation page should have an error`` =
    testAsync "Validation page should have an error" {
        let deps =
            Api.ProcessRequestDeps.Russian
                { processRequestDeps with
                    postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_has_error" }

        let! responseRes = request |> Api.processRequest deps

        let response = Expect.wantOk responseRes "getAppointments response should be Ok"

        match response.State with
        | Domain.RequestState.Failed(Operation reason) ->
            Expect.equal reason.Code (Some ErrorCodes.PageHasError) $"Error code should be {ErrorCodes.PageHasError}"
        | error -> Expect.isTrue false $"Error should be {nameof Operation} type, but was {error}"
    }

let private ``validation page should have a confirmation request`` =
    testAsync "Validation page should have a confirmation request" {
        let deps =
            Api.ProcessRequestDeps.Russian
                { processRequestDeps with
                    postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_requires_confirmation" }

        let! responseRes = request |> Api.processRequest deps

        let response = Expect.wantOk responseRes "getAppointments response should be Ok"

        match response.State with
        | Domain.RequestState.Failed(Operation reason) ->
            Expect.equal reason.Code (Some ErrorCodes.NotConfirmed) $"Error code should be {ErrorCodes.NotConfirmed}"
        | error -> Expect.isTrue false $"Error should be {nameof Operation} type, but was {error}"
    }

let private ``appointments page should not have data`` =
    testTheoryAsync "Appointments page should not have data" [ 1; 2; 3; 4; 5; 6; 7 ]
    <| fun i ->
        async {
            let deps =
                Api.ProcessRequestDeps.Russian
                    { processRequestDeps with
                        postAppointmentsPage = fun _ _ _ -> httpPostStringRequest $"appointments_page_empty_result_{i}" }

            let! responseRes = request |> Api.processRequest deps
            let request = Expect.wantOk responseRes "Appointments should be Ok"
            Expect.isEmpty request.Appointments "Appointments should not be Some"
        }

let private ``appointments page should have data`` =
    testTheoryAsync "Appointments page should have data" [ 1; 2; 3 ]
    <| fun i ->
        async {
            let deps =
                Api.ProcessRequestDeps.Russian
                    { processRequestDeps with
                        postAppointmentsPage = fun _ _ _ -> httpPostStringRequest $"appointments_page_has_result_{i}" }

            let! responseRes = request |> Api.processRequest deps
            let request = Expect.wantOk responseRes "Appointments should be Ok"
            Expect.isTrue (not request.Appointments.IsEmpty) "Appointments should be not empty"
        }

let list =
    testList
        "Russian"
        [ ``validation page should have a confirmation request``
          ``validation page should have an error``
          ``appointments page should not have data``
          ``appointments page should have data`` ]
