module EA.Embassies.Russian.Kdmid.Tests

open System
open Expecto
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Embassies.Russian.Domain
open EA.Embassies.Russian.Kdmid.Domain

module private Fixture =
    open Web.Http.Domain.Response
    open Persistence.FileSystem
    open EA.Embassies.Russian.Kdmid.Dependencies

    let httpRequestHeaders =
        Some
        <| Map [ "Set-Cookie", [ "ASP.NET_SessionId=1"; " AlteonP=1"; " __ddg1_=1" ] ]

    let httpGetStringRequest fileName =
        { FilePath = "./test_data/"
          FileName = $"{fileName}.html" }
        |> Storage.init
        |> ResultAsync.wrap Read.string
        |> ResultAsync.map (Option.defaultValue "")
        |> ResultAsync.map (fun data ->
            { Content = data
              Headers = httpRequestHeaders
              StatusCode = 200 })

    let httpGetBytesRequest fileName =
        { FilePath = "./test_data/"
          FileName = fileName }
        |> Storage.init
        |> ResultAsync.wrap Read.bytes
        |> ResultAsync.map (Option.defaultValue [||])
        |> ResultAsync.map (fun data ->
            { Content = data
              Headers = httpRequestHeaders
              StatusCode = 200 })

    let httpPostStringRequest fileName =
        { FilePath = "./test_data/"
          FileName = $"{fileName}.html" }
        |> Storage.init
        |> ResultAsync.wrap Read.string
        |> ResultAsync.map (Option.defaultValue "")

    let Dependencies: Order.Dependencies =
        { RestartAttempts = 1
          updateRequest = fun request -> async { return Ok request }
          getInitialPage = fun _ _ -> httpGetStringRequest "initial_page_response"
          getCaptcha = fun _ _ -> httpGetBytesRequest "captcha.png"
          solveIntCaptcha = fun _ -> async { return Ok 42 }
          postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_valid_response"
          postAppointmentsPage = fun _ _ _ -> httpPostStringRequest "appointments_page_has_result_1"
          postConfirmationPage = fun _ _ _ -> httpPostStringRequest "confirmation_page_has_result_1" }

    let private KdmidRequest =
        { Uri = Uri("https://belgrad.kdmid.ru/queue/OrderInfo.aspx?id=96794&cd=AB6C2AF3")
          Embassy =
            { Id = "EMB.RUS.SRB.BEG" |> Graph.NodeIdValue
              Name = [ "Russian"; "Germany"; "Berlin" ] |> Graph.Node.Name.combine
              ShortName = "Berlin"
              Description = None
              TimeZone = None }
          Service =
            { Id = Graph.NodeId.New
              Name = "TestService"
              ShortName = "TestService"
              Instruction = None
              Description = None }
          SubscriptionState = Manual
          ConfirmationState = ConfirmationState.Auto FirstAvailable }

    let IssueForeign =
        { KdmidService.Request = KdmidRequest.ToRequest()
          KdmidService.Dependencies = Dependencies }

open Fixture

let private ``validation page should have an error`` =
    testAsync "Validation page should have an error" {
        let dependencies =
            { Dependencies with
                postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_has_error" }

        let service =
            { IssueForeign with
                KdmidService.Dependencies = dependencies }
            |> Kdmid

        let! serviceResult = EA.Embassies.Russian.API.Service.get service

        let error = Expect.wantError serviceResult "processed service should be an error"

        match error with
        | Operation { Code = Some(Custom Constants.ErrorCode.PAGE_HAS_ERROR) } -> ()
        | _ -> Expect.isTrue false $"Error code should be {Constants.ErrorCode.PAGE_HAS_ERROR}"
    }

let private ``validation page should have a confirmed request`` =
    testAsync "Validation page should have a confirmed request" {
        let dependencies =
            { Dependencies with
                postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_requires_confirmation" }

        let service =
            { IssueForeign with
                KdmidService.Dependencies = dependencies }
            |> Kdmid

        let! serviceResult = EA.Embassies.Russian.API.Service.get service

        let error = Expect.wantError serviceResult "processed service should be an error"

        match error with
        | Operation { Code = Some(Custom Constants.ErrorCode.NOT_CONFIRMED) } -> ()
        | _ -> Expect.isTrue false $"Error code should be {Constants.ErrorCode.NOT_CONFIRMED}"
    }

let private ``validation page should have a confirmation`` =
    testAsync "Validation page should have a confirmation" {
        let dependencies =
            { Dependencies with
                postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_has_confirmation" }

        let service =
            { IssueForeign with
                KdmidService.Dependencies = dependencies }
            |> Kdmid

        let! serviceResult = EA.Embassies.Russian.API.Service.get service

        let error = Expect.wantError serviceResult "processed service should be an error"

        match error with
        | Operation { Code = Some(Custom Constants.ErrorCode.CONFIRMATION_EXISTS) } -> ()
        | _ -> Expect.isTrue false $"Error code should be {Constants.ErrorCode.CONFIRMATION_EXISTS}"
    }

let private ``validation page should have a deleted request`` =
    testAsync "Validation page should have a deleted request" {
        let dependencies =
            { Dependencies with
                postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_request_deleted" }

        let service =
            { IssueForeign with
                KdmidService.Dependencies = dependencies }
            |> Kdmid

        let! serviceResult = EA.Embassies.Russian.API.Service.get service

        let error = Expect.wantError serviceResult "processed service should be an error"

        match error with
        | Operation { Code = Some(Custom Constants.ErrorCode.REQUEST_DELETED) } -> ()
        | _ -> Expect.isTrue false $"Error code should be {Constants.ErrorCode.REQUEST_DELETED}"
    }

let private ``appointments page should not have data`` =
    testTheoryAsync "Appointments page should not have data" [ 1 ]
    <| fun i ->
        async {
            let dependencies =
                { Dependencies with
                    postAppointmentsPage = fun _ _ _ -> httpPostStringRequest $"appointments_page_empty_result_{i}" }

            let service =
                { IssueForeign with
                    KdmidService.Dependencies = dependencies }
                |> Kdmid

            let! serviceResult = EA.Embassies.Russian.API.Service.get service

            let result = Expect.wantOk serviceResult "Appointments should be Ok"
            Expect.isEmpty result.Appointments "Appointments should not be not empty"
        }

let private ``appointments page should have data`` =
    testTheoryAsync "Appointments page should have data" [ 1; 2; 3 ]
    <| fun i ->
        async {
            let dependencies =
                { Dependencies with
                    postAppointmentsPage = fun _ _ _ -> httpPostStringRequest $"appointments_page_has_result_{i}" }

            let service =
                { IssueForeign with
                    KdmidService.Dependencies = dependencies }
                |> Kdmid

            let! serviceResult = EA.Embassies.Russian.API.Service.get service

            let result = Expect.wantOk serviceResult "Appointments should be Ok"
            Expect.isTrue (not result.Appointments.IsEmpty) "Appointments should be not empty"
        }

let private ``confirmation page should have a valid result`` =
    testTheoryAsync "Confirmation page should have a valid result" [ 1; 2 ]
    <| fun i ->
        async {
            let dependencies =
                { Dependencies with
                    postConfirmationPage = fun _ _ _ -> httpPostStringRequest $"confirmation_page_has_result_{i}" }

            let service =
                { IssueForeign with
                    KdmidService.Dependencies = dependencies }
                |> Kdmid

            let! serviceResult = EA.Embassies.Russian.API.Service.get service

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

let list =
    testList
        "Kdmid"
        [ //``validation page should have an error``
          //``validation page should have a confirmed request``
          //``validation page should have a confirmation``
          //``validation page should have a deleted request``
          ``appointments page should not have data``
          //``appointments page should have data``
          //``confirmation page should have a valid result``
          ]
