[<RequireQualifiedAccess>]
module internal EA.Embassies.Russian.API

open EA.Core.Domain
open EA.Embassies.Russian.Domain
open EA.Embassies.Russian.Core

let handleService service =
    match service with
    | Passport service ->
        match service with
        | IssueForeignPassport service -> Passport.handle service
        | CheckPassportStatus service -> Passport.handle service
        | TakeReadyPassport service -> Passport.handle service
    | Notary service ->
        match service with
        | CertPowerAttorney service -> Notary.handle service
    | Citizenship service ->
        match service with
        | RenunciationOfCitizenship service -> Citizenship.handle service

let validateRequest request =
    request.Payload
    |> createCredentials
    |> Result.bind (Request.validateCredentials request)
    |> Result.map (fun _ -> ())

let processRequest deps request =

    // define
    let setRequestInProcessState = Request.setInProcessState deps
    let createRequestCredentials = Request.createCredentials
    let createHttpClient = Http.createClient
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

let getCountries () =
    Set
        [ Albania <| Tirana
          Bosnia <| Sarajevo
          Finland <| Helsinki
          France <| Paris
          Germany <| Berlin
          Hungary <| Budapest
          Ireland <| Dublin
          Montenegro <| Podgorica
          Netherlands <| Hague
          Serbia <| Belgrade
          Slovenia <| Ljubljana
          Switzerland <| Bern ]
