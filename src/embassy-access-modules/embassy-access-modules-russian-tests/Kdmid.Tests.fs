﻿module EA.Embassies.Russian.Kdmid.Tests

open System
open Expecto
open Infrastructure
open EA.Core.Domain
open EA.Embassies.Russian.Domain
open EA.Embassies.Russian.Kdmid.Domain

module private Fixture =
    open Web.Http.Domain
    open EA.Core.Domain.Constants
    open Persistence.FileSystem

    let httpRequestHeaders =
        Some
        <| Map [ "Set-Cookie", [ "ASP.NET_SessionId=1"; " AlteonP=1"; " __ddg1_=1" ] ]

    let httpGetStringRequest fileName =
        $"./test_data/{fileName}.html"
        |> Storage.init
        |> ResultAsync.wrap Storage.Read.string
        |> ResultAsync.map (Option.defaultValue "")
        |> ResultAsync.map (fun data ->
            { Content = data
              Headers = httpRequestHeaders
              StatusCode = 200 })

    let httpGetBytesRequest fileName =
        $"./test_data/{fileName}"
        |> Storage.init
        |> ResultAsync.wrap Storage.Read.bytes
        |> ResultAsync.map (Option.defaultValue [||])
        |> ResultAsync.map (fun data ->
            { Content = data
              Headers = httpRequestHeaders
              StatusCode = 200 })

    let httpPostStringRequest fileName =
        $"./test_data/{fileName}.html"
        |> Storage.init
        |> ResultAsync.wrap Storage.Read.string
        |> ResultAsync.map (Option.defaultValue "")

    let Dependencies =
        { updateRequest = fun request -> async { return Ok request }
          getInitialPage = fun _ _ -> httpGetStringRequest "initial_page_response"
          getCaptcha = fun _ _ -> httpGetBytesRequest "captcha.png"
          solveCaptcha = fun _ -> async { return Ok 42 }
          postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_valid_response"
          postAppointmentsPage = fun _ _ _ -> httpPostStringRequest "appointments_page_has_result_1"
          postConfirmationPage = fun _ _ _ -> httpPostStringRequest "confirmation_page_has_result_1" }

    let private Request =
        { Uri = Uri("https://berlin.kdmid.ru/queue/orderinfo.aspx?id=290383&cd=B714253F")
          Embassy =
            { Id = Graph.NodeId.New
              Name = [ Embassy.RUSSIAN; Country.GERMANY; City.BERLIN ] |> Graph.buildNodeNameOfSeq
              Description = None }
          TimeZone = 1.0
          Confirmation = Auto FirstAvailable }

    let IssueForeign =
        { Service.Request = Request
          Service.Dependencies = Dependencies }

open Fixture

let private ``validation page should have an error`` =
    testAsync "Validation page should have an error" {
        let dependencies =
            { Dependencies with
                postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_has_error" }

        let service =
            { IssueForeign with
                Service.Dependencies = dependencies }
            |> Service.Kdmid

        let! serviceResult = EA.Embassies.Russian.API.Service.get service "Test"

        let error = Expect.wantError serviceResult "processed service should be an error"

        match error with
        | Operation { Code = Some Constants.ErrorCode.PAGE_HAS_ERROR } -> ()
        | _ -> Expect.isTrue false $"Error code should be {Constants.ErrorCode.PAGE_HAS_ERROR}"
    }

let private ``validation page should have a confirmed request`` =
    testAsync "Validation page should have a confirmed request" {
        let dependencies =
            { Dependencies with
                postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_requires_confirmation" }

        let service =
            { IssueForeign with
                Service.Dependencies = dependencies }
            |> Service.Kdmid

        let! serviceResult = EA.Embassies.Russian.API.Service.get service "Test"

        let error = Expect.wantError serviceResult "processed service should be an error"

        match error with
        | Operation { Code = Some Constants.ErrorCode.NOT_CONFIRMED } -> ()
        | _ -> Expect.isTrue false $"Error code should be {Constants.ErrorCode.NOT_CONFIRMED}"
    }

let private ``validation page should have a confirmation`` =
    testAsync "Validation page should have a confirmation" {
        let dependencies =
            { Dependencies with
                postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_has_confirmation" }

        let service =
            { IssueForeign with
                Service.Dependencies = dependencies }
            |> Service.Kdmid

        let! serviceResult = EA.Embassies.Russian.API.Service.get service "Test"

        let error = Expect.wantError serviceResult "processed service should be an error"

        match error with
        | Operation { Code = Some Constants.ErrorCode.CONFIRMATION_EXISTS } -> ()
        | _ -> Expect.isTrue false $"Error code should be {Constants.ErrorCode.CONFIRMATION_EXISTS}"
    }

let private ``validation page should have a deleted request`` =
    testAsync "Validation page should have a deleted request" {
        let dependencies =
            { Dependencies with
                postValidationPage = fun _ _ _ -> httpPostStringRequest "validation_page_request_deleted" }

        let service =
            { IssueForeign with
                Service.Dependencies = dependencies }
            |> Service.Kdmid

        let! serviceResult = EA.Embassies.Russian.API.Service.get service "Test"

        let error = Expect.wantError serviceResult "processed service should be an error"

        match error with
        | Operation { Code = Some Constants.ErrorCode.REQUEST_DELETED } -> ()
        | _ -> Expect.isTrue false $"Error code should be {Constants.ErrorCode.REQUEST_DELETED}"
    }

let private ``appointments page should not have data`` =
    testTheoryAsync "Appointments page should not have data" [ 1; 2; 3; 4; 5; 6; 7 ]
    <| fun i ->
        async {
            let dependencies =
                { Dependencies with
                    postAppointmentsPage = fun _ _ _ -> httpPostStringRequest $"appointments_page_empty_result_{i}" }

            let service =
                { IssueForeign with
                    Service.Dependencies = dependencies }
                |> Service.Kdmid

            let! serviceResult = EA.Embassies.Russian.API.Service.get service "Test"

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
                    Service.Dependencies = dependencies }
                |> Service.Kdmid

            let! serviceResult = EA.Embassies.Russian.API.Service.get service "Test"

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
                    Service.Dependencies = dependencies }
                |> Service.Kdmid

            let! serviceResult = EA.Embassies.Russian.API.Service.get service "Test"

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
        [ ``validation page should have an error``
          ``validation page should have a confirmed request``
          ``validation page should have a confirmation``
          ``validation page should have a deleted request``
          ``appointments page should not have data``
          ``appointments page should have data``
          ``confirmation page should have a valid result`` ]
