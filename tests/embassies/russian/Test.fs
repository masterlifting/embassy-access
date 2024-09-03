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
          ConfirmationState = Auto <| FirstAvailable
          Appointments = Set.empty
          Description = None
          GroupBy = None
          Modified = DateTime.UtcNow }

    let requiredHeaders =
        Some
        <| Map [ "Set-Cookie", [ "ASP.NET_SessionId=1"; " AlteonP=1"; " __ddg1_=1" ] ]

    let httpGetStringRequest fileName =
        $"./embassies/russian/test_data/{fileName}.html"
        |> Storage.create
        |> ResultAsync.wrap Storage.Read.string
        |> ResultAsync.map (fun data ->
            { Content = data
              Headers = requiredHeaders
              StatusCode = 200 })

    let httpPostStringRequest fileName =
        $"./embassies/russian/test_data/{fileName}.html"
        |> Storage.create
        |> ResultAsync.wrap Storage.Read.string

    let httpGetBytesRequest fileName =
        $"./embassies/russian/test_data/{fileName}"
        |> Storage.create
        |> ResultAsync.wrap Storage.Read.bytes
        |> ResultAsync.map (fun data ->
            { Content = data
              Headers = requiredHeaders
              StatusCode = 200 })

    let processRequestDeps =
        { Configuration = { TimeShift = 0y }
          updateRequest = fun r -> async { return Ok r }
          getInitialPage = fun _ _ -> httpGetStringRequest "initial_page_response"
          getCaptcha = fun _ _ -> httpGetBytesRequest "captcha.png"
          solveCaptcha = fun _ -> async { return Ok 42 }
          postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_valid_response"
          postAppointmentsPage = fun _ _ _ -> httpPostStringRequest "appointments_page_has_result_1"
          postConfirmationPage = fun _ _ _ -> httpPostStringRequest "confirmation_page_has_result_1" }

open Fixture

let private ``validation page should have an error`` =
    testAsync "Validation page should have an error" {
        let deps =
            Api.ProcessRequestDeps.Russian
                { processRequestDeps with
                    postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_has_error" }

        let! responseRes = request |> Api.processRequest deps

        let response = Expect.wantOk responseRes "processRequest should be Ok"

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

        let! requestRes = request |> Api.processRequest deps
        let request = Expect.wantOk requestRes "processRequest should be Ok"

        match request.State with
        | Domain.RequestState.Failed(Operation reason) ->
            Expect.equal reason.Code (Some ErrorCodes.NotConfirmed) $"Error code should be {ErrorCodes.NotConfirmed}"
        | error -> Expect.isTrue false $"Error should be {nameof Operation} type, but was {error}"
    }

let private ``validation page shold have a confirmation`` =
    testAsync "Validation page should have a confirmation" {
        let deps =
            Api.ProcessRequestDeps.Russian
                { processRequestDeps with
                    postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_has_confirmation" }

        let! requestRes = request |> Api.processRequest deps
        let request = Expect.wantOk requestRes "processRequest should be Ok"

        match request.State with
        | Domain.RequestState.Failed(Operation reason) ->
            Expect.equal
                reason.Code
                (Some ErrorCodes.ConfirmationExists)
                $"Error code should be {ErrorCodes.ConfirmationExists}"
        | error -> Expect.isTrue false $"Error should be {nameof Operation} type, but was {error}"
    }

let private ``validation page should have a deleted request`` =
    testAsync "Validation page should have a deleted request" {
        let deps =
            Api.ProcessRequestDeps.Russian
                { processRequestDeps with
                    postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_request_deleted" }

        let! requestRes = request |> Api.processRequest deps
        let request = Expect.wantOk requestRes "processRequest should be Ok"

        match request.State with
        | Domain.RequestState.Failed(Operation reason) ->
            Expect.equal
                reason.Code
                (Some ErrorCodes.RequestDeleted)
                $"Error code should be {ErrorCodes.RequestDeleted}"
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

            let! requestRes = request |> Api.processRequest deps
            let request = Expect.wantOk requestRes "Appointments should be Ok"
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

            let! requestRes = request |> Api.processRequest deps
            let request = Expect.wantOk requestRes "Appointments should be Ok"
            Expect.isTrue (not request.Appointments.IsEmpty) "Appointments should be not empty"
        }

let private ``confirmation page should have a valid result`` =
    testTheoryAsync "Confirmation page should have a valid result" [ 1; 2 ]
    <| fun i ->
        async {
            let deps =
                Api.ProcessRequestDeps.Russian
                    { processRequestDeps with
                        postConfirmationPage = fun _ _ _ -> httpPostStringRequest $"confirmation_page_has_result_{i}" }

            let! requestRes = request |> Api.processRequest deps
            let request = Expect.wantOk requestRes "Appointments should be Ok"

            match request.State with
            | Domain.RequestState.Failed error ->
                Expect.isTrue false $"Request should have valid state, but was Failed. Error: {error.Message}"
            | Domain.RequestState.Completed _ ->
                let confirmation = request.Appointments |> Seq.tryPick (_.Confirmation)

                Expect.wantSome confirmation "Confirmation should be Some" |> ignore
            | _ ->
                Expect.isTrue
                    false
                    $"Request should have valid state, but was {request.State} with description {request.Description}"
        }

let list =
    testList
        "Russian"
        [ ``validation page should have an error``
          ``validation page should have a confirmation request``
          ``validation page shold have a confirmation``
          ``validation page should have a deleted request``
          ``appointments page should not have data``
          ``appointments page should have data``
          ``confirmation page should have a valid result`` ]
