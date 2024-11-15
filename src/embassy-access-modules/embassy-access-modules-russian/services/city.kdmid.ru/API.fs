module internal EA.Embassies.Russian.Kdmid.API

open EA.Embassies.Russian.Kdmid.Domain
open EA.Embassies.Russian.Kdmid

let processRequest deps request =

    // define
    let setRequestInProcessState = Request.setInProcessState deps
    let createRequestCredentials = Request.createCredentials
    let createHttpClient = Web.Http.createClient
    let processInitialPage = InitialPage.handle deps
    let setRequestAttempt = Request.setAttempt deps
    let processValidationPage = ValidationPage.handle deps
    let processAppointmentsPage = AppointmentsPage.handle deps
    let processConfirmationPage = ConfirmationPage.handle deps
    let setRequestProcessedState = Request.setProcessedState deps request

    // pipe
    let startProcess =
        setRequestInProcessState
        >> createRequestCredentials
        >> createHttpClient
        >> processInitialPage
        >> setRequestAttempt
        >> processValidationPage
        >> processAppointmentsPage
        >> processConfirmationPage
        >> setRequestProcessedState

    request |> startProcess

let SupportedCountries = Constants.SUPPORTED__SUB_DOMAINS.Values |> set
