module EA.Russian.Tests.Kdmid

open System
open Expecto
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain
open EA.Core.Domain
open EA.Russian.Services.Kdmid
open EA.Russian.Services.Domain.Kdmid

module private Fixture =
    open Persistence.Storages
    open Persistence.Storages.Domain.FileSystem

    let httpRequestHeaders =
        Some
        <| Map [ "Set-Cookie", [ "ASP.NET_SessionId=1"; " AlteonP=1"; " __ddg1_=1" ] ]

    let httpGetStringRequest fileName =
        {
            FilePath = "./test_data/"
            FileName = $"{fileName}.html"
        }
        |> FileSystem.Provider.init
        |> ResultAsync.wrap FileSystem.Read.string
        |> ResultAsync.map (Option.defaultValue "")
        |> ResultAsync.map (fun data -> {
            Http.Response.Content = data
            Http.Response.Headers = httpRequestHeaders
            Http.Response.StatusCode = 200
        })

    let httpGetBytesRequest fileName =
        {
            FilePath = "./test_data/"
            FileName = fileName
        }
        |> FileSystem.Provider.init
        |> ResultAsync.wrap FileSystem.Read.bytes
        |> ResultAsync.map (Option.defaultValue [||])
        |> ResultAsync.map (fun data -> {
            Http.Response.Content = data
            Http.Response.Headers = httpRequestHeaders
            Http.Response.StatusCode = 200
        })

    let httpPostStringRequest fileName =
        {
            FilePath = "./test_data/"
            FileName = $"{fileName}.html"
        }
        |> FileSystem.Provider.init
        |> ResultAsync.wrap FileSystem.Read.string
        |> ResultAsync.map (Option.defaultValue "")

    let Client = {
        initHttpClient =
            fun _ ->
                Web.Clients.Http.Provider.init {
                    Host = "https://belgrad.kdmid.ru"
                    Headers = None
                }
        updateRequest = fun request -> async { return Ok request }
        getInitialPage = fun _ _ -> httpGetStringRequest "initial_page_response"
        getCaptcha = fun _ _ -> httpGetBytesRequest "captcha.png"
        solveIntCaptcha = fun _ -> async { return Ok 42 }
        postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_valid_response"
        postAppointmentsPage = fun _ _ _ -> httpPostStringRequest "appointments_page_has_result_1"
        postConfirmationPage = fun _ _ _ -> httpPostStringRequest "confirmation_page_has_result_1"
    }

    let Request = {
        Id = RequestId.createNew ()
        Service = {
            Id = Graph.NodeId.createNew ()
            Name = "TestService"
            Payload = "https://belgrad.kdmid.ru/queue/OrderInfo.aspx?id=96794&cd=AB6C2AF3"
            Description = None
            Embassy = {
                Id = "EMB.RUS.SRB.BEG" |> Graph.NodeIdValue
                Name = "Berlin"
                Description = None
                TimeZone = 0.
            }
        }
        ProcessState = Ready
        IsBackground = false
        Limits = Set.empty<Limit>
        ConfirmationState = FirstAvailable
        Appointments = Set.empty<Appointment>
        Modified = DateTime.UtcNow
    }

open Fixture

let private ``validation page should have an error`` =
    testAsync "Validation page should have an error" {
        let client = {
            Client with
                postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_has_error"
        }

        let! serviceResult = client |> Service.tryProcess Request

        let error = Expect.wantError serviceResult "processed service should be an error"

        match error with
        | Operation {
                        Code = Some(Custom Constants.ErrorCode.PAGE_HAS_ERROR)
                    } -> ()
        | _ -> Expect.isTrue false $"Error code should be {Constants.ErrorCode.PAGE_HAS_ERROR}"
    }

let private ``validation page should have a confirmed request`` =
    testAsync "Validation page should have a confirmed request" {
        let client = {
            Client with
                postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_requires_confirmation"
        }

        let! serviceResult = client |> Service.tryProcess Request

        let error = Expect.wantError serviceResult "processed service should be an error"

        match error with
        | Operation {
                        Code = Some(Custom Constants.ErrorCode.REQUEST_NOT_CONFIRMED)
                    } -> ()
        | _ -> Expect.isTrue false $"Error code should be {Constants.ErrorCode.REQUEST_NOT_CONFIRMED}"
    }

let private ``validation page should have a confirmation`` =
    testAsync "Validation page should have a confirmation" {
        let client = {
            Client with
                postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_has_confirmation"
        }

        let! serviceResult = client |> Service.tryProcess Request

        let error = Expect.wantError serviceResult "processed service should be an error"

        match error with
        | Operation {
                        Code = Some(Custom Constants.ErrorCode.REQUEST_AWAITING_LIST)
                    } -> ()
        | _ -> Expect.isTrue false $"Error code should be {Constants.ErrorCode.REQUEST_AWAITING_LIST}"
    }

let private ``validation page should have a deleted request`` =
    testAsync "Validation page should have a deleted request" {
        let client = {
            Client with
                postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_request_deleted"
        }

        let! serviceResult = client |> Service.tryProcess Request

        let error = Expect.wantError serviceResult "processed service should be an error"

        match error with
        | Operation {
                        Code = Some(Custom Constants.ErrorCode.REQUEST_DELETED)
                    } -> ()
        | _ -> Expect.isTrue false $"Error code should be {Constants.ErrorCode.REQUEST_DELETED}"
    }

let private ``appointments page should not have data`` =
    testTheoryAsync "Appointments page should not have data" [ 1 ]
    <| fun i ->
        async {
            let client = {
                Client with
                    postAppointmentsPage = fun _ _ _ -> httpPostStringRequest $"appointments_page_empty_result_{i}"
            }

            let! serviceResult = client |> Service.tryProcess Request

            let result = Expect.wantOk serviceResult "Appointments should be Ok"
            Expect.isEmpty result.Appointments "Appointments should not be not empty"
        }

let private ``appointments page should have data`` =
    testTheoryAsync "Appointments page should have data" [ 1; 2; 3 ]
    <| fun i ->
        async {
            let client = {
                Client with
                    postAppointmentsPage = fun _ _ _ -> httpPostStringRequest $"appointments_page_has_result_{i}"
            }

            let! serviceResult = client |> Service.tryProcess Request

            let result = Expect.wantOk serviceResult "Appointments should be Ok"
            Expect.isTrue (not result.Appointments.IsEmpty) "Appointments should be not empty"
        }

let private ``confirmation page should have a valid result`` =
    testTheoryAsync "Confirmation page should have a valid result" [ 1; 2 ]
    <| fun i ->
        async {
            let client = {
                Client with
                    postConfirmationPage = fun _ _ _ -> httpPostStringRequest $"confirmation_page_has_result_{i}"
            }

            let! serviceResult = client |> Service.tryProcess Request

            let result = Expect.wantOk serviceResult "Appointments should be Ok"

            match result.ProcessState with
            | ProcessState.Failed error ->
                Expect.isTrue false $"Service request should have a valid state, but was Failed. Error: {error.Message}"
            | ProcessState.Completed _ ->
                let confirmation = result.Appointments |> Seq.tryPick _.Confirmation

                Expect.wantSome confirmation "Confirmation should be Some" |> ignore
            | _ ->
                Expect.isTrue
                    false
                    $"Service request should have a valid state, but was {result.ProcessState} with description {result.Service.Description}"
        }

let tests =
    testList "Kdmid" [
        ``validation page should have an error``
        ``validation page should have a confirmed request``
        ``validation page should have a confirmation``
        ``validation page should have a deleted request``
        ``appointments page should not have data``
        ``appointments page should have data``
        ``confirmation page should have a valid result``
    ]
