module EA.Embassies.Russian.Kdmid.API

open EA.Core.Domain
open EA.Embassies.Russian.Kdmid.Domain
open EA.Embassies.Russian.Kdmid

let validateRequest (request: EA.Core.Domain.Request) =
    request.Service.Payload
    |> createCredentials
    |> Result.bind (Request.validateCredentials request)
    |> Result.map (fun _ -> ())

let processRequest request deps =

    // define
    let setRequestInProcessState = Request.setInProcessState deps
    let createRequestCredentials = Request.createCredentials
    let createHttpClient = Web.Http.createClient
    let processInitialPage = InitialPage.handle deps
    let setRequestAttempt = Request.setAttempt deps
    let processValidationPage = ValidationPage.handle deps
    let processAppointmentsPage = AppointmentsPage.handle deps
    let processConfirmationPage = ConfirmationPage.handle deps
    let setRequestFinalState = Request.completeConfirmation deps request

    // pipe
    let start =
        setRequestInProcessState
        >> createRequestCredentials
        >> createHttpClient
        >> processInitialPage
        >> setRequestAttempt
        >> processValidationPage
        >> processAppointmentsPage
        >> processConfirmationPage
        >> setRequestFinalState

    request |> start

let getCountries () = Constants.SUPPORTED_DOMAINS.Values |> set
