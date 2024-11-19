[<RequireQualifiedAccess>]
module EA.Embassies.Russian.Kdmid.Request

open System
open EA.Embassies.Russian.Kdmid.Web
open EA.Persistence
open Infrastructure
open EA.Core.Domain
open EA.Embassies.Russian.Kdmid.Domain

let createDependencies ct storage =
    { updateRequest = fun request -> storage |> Repository.Command.Request.update request ct
      createHttpClient = Http.createKdmidClient
      getInitialPage =
        fun request client ->
            client
            |> Web.Http.Client.Request.get ct request
            |> Web.Http.Client.Response.String.read ct
      getCaptcha =
        fun request client ->
            client
            |> Web.Http.Client.Request.get ct request
            |> Web.Http.Client.Response.Bytes.read ct
      solveCaptcha = Web.Captcha.solveToInt ct
      postValidationPage =
        fun request content client ->
            client
            |> Web.Http.Client.Request.post ct request content
            |> Web.Http.Client.Response.String.readContent ct
      postAppointmentsPage =
        fun request content client ->
            client
            |> Web.Http.Client.Request.post ct request content
            |> Web.Http.Client.Response.String.readContent ct
      postConfirmationPage =
        fun request content client ->
            client
            |> Web.Http.Client.Request.post ct request content
            |> Web.Http.Client.Response.String.readContent ct }

let private validateCity (request: EA.Core.Domain.Request) credentials =
    match request.Service.Embassy.Country.City = credentials.Country.City with
    | true -> Ok credentials
    | false ->
        Error
        <| NotSupported $"Requested {request.Service.Embassy.Country.City} for the subdomain {credentials.SubDomain}"

let private createPayload (uri: Uri) =
    match uri.Host.Split '.' with
    | hostParts when hostParts.Length < 3 -> uri.Host |> NotSupported |> Error
    | hostParts ->
        let credentials = ResultBuilder()

        credentials {
            let subDomain = hostParts[0]

            let! country =
                match Constants.SUPPORTED__SUB_DOMAINS |> Map.tryFind subDomain with
                | Some country -> Ok country
                | None -> subDomain |> NotSupported |> Error

            let! queryParams = uri |> Web.Http.Client.Route.toQueryParams

            let! id =
                queryParams
                |> Map.tryFind "id"
                |> Option.map (function
                    | AP.IsInt id when id > 1000 -> id |> Ok
                    | _ -> "id query parameter" |> NotSupported |> Error)
                |> Option.defaultValue ("id query parameter" |> NotFound |> Error)

            let! cd =
                queryParams
                |> Map.tryFind "cd"
                |> Option.map (function
                    | AP.IsLettersOrNumbers cd -> cd |> Ok
                    | _ -> "cd query parameter" |> NotSupported |> Error)
                |> Option.defaultValue ("cd query parameter" |> NotFound |> Error)

            let! ems =
                queryParams
                |> Map.tryFind "ems"
                |> Option.map (function
                    | AP.IsLettersOrNumbers ems -> ems |> Some |> Ok
                    | _ -> "ems query parameter" |> NotSupported |> Error)
                |> Option.defaultValue (None |> Ok)

            return
                { Country = country
                  SubDomain = subDomain
                  Id = id
                  Cd = cd
                  Ems = ems }
        }

let private parsePayload =
    ResultAsync.bind (fun request ->
        request.Service.Payload
        |> Uri
        |> createPayload
        |> Result.bind (validateCity request)
        |> Result.map (fun credentials -> credentials, request))

let private setInProcessState deps request =
    deps.updateRequest
        { request with
            ProcessState = InProcess
            Modified = DateTime.UtcNow }

let private setAttemptCore timeZone request =
    let modified, attempt = request.Attempt
    let modified = modified.AddHours timeZone
    let today = DateTime.UtcNow.AddHours timeZone

    match modified.DayOfYear = today.DayOfYear, attempt > 20 with
    | true, true ->
        Error
        <| Canceled $"The request was cancelled due to the number of attempts reached the %i{attempt}."
    | true, false ->
        Ok
        <| { request with
               Attempt = DateTime.UtcNow, attempt + 1 }
    | _ ->
        Ok
        <| { request with
               Attempt = DateTime.UtcNow, 1 }

let private setAttempt timeZone deps =
    ResultAsync.bindAsync (fun (httpClient, queryParams, formData, request) ->
        request
        |> setAttemptCore timeZone
        |> ResultAsync.wrap deps.updateRequest
        |> ResultAsync.map (fun request -> httpClient, queryParams, formData, request))

let private setCompletedState deps request =
    let message =
        match request.Appointments.IsEmpty with
        | true -> "No appointments found"
        | false ->
            match request.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
            | [] -> $"Found appointments: %i{request.Appointments.Count}"
            | confirmations -> $"Found confirmations: %i{confirmations.Length}"

    deps.updateRequest
        { request with
            ProcessState = Completed message
            Modified = DateTime.UtcNow }

let private setFailedState error deps request =
    let attempt =
        match error with
        | Operation { Code = Some Web.Captcha.CaptchaErrorCode } -> request.Attempt
        | _ -> DateTime.UtcNow, snd request.Attempt + 1

    deps.updateRequest
        { request with
            ProcessState = Failed error
            Attempt = attempt
            Modified = DateTime.UtcNow }
    |> ResultAsync.bind (fun _ -> Error <| error.add $"Payload: %s{request.Service.Payload}")

let private setProcessedState deps request confirmation =
    async {
        match! confirmation with
        | Error error -> return! request |> setFailedState error deps
        | Ok request -> return! request |> setCompletedState deps
    }

let start deps timeZone request =

    // define
    let setInProcessState = setInProcessState deps
    let parsePayload = parsePayload
    let createHttpClient = Http.createClient
    let processInitialPage = InitialPage.handle deps
    let setAttempt = setAttempt timeZone deps
    let processValidationPage = ValidationPage.handle deps
    let processAppointmentsPage = AppointmentsPage.handle deps
    let processConfirmationPage = ConfirmationPage.handle deps
    let setProcessedState = setProcessedState deps request

    // pipe
    let run =
        setInProcessState
        >> parsePayload
        >> createHttpClient
        >> processInitialPage
        >> setAttempt
        >> processValidationPage
        >> processAppointmentsPage
        >> processConfirmationPage
        >> setProcessedState

    request |> run

let errorFilter error =
    match error with
    | Operation reason ->
        match reason.Code with
        | Some Constants.ErrorCodes.CONFIRMATION_EXISTS
        | Some Constants.ErrorCodes.NOT_CONFIRMED
        | Some Constants.ErrorCodes.REQUEST_DELETED -> true
        | _ -> false
    | _ -> false


let pick deps notify data =

    let rec innerLoop (errors: Error' list) (data: (float * EA.Core.Domain.Request) list) =
        async {
            match data with
            | [] ->
                return
                    match errors.Length with
                    | 0 -> Error [ "Requests to handle" |> NotFound ]
                    | _ -> Error errors
            | (timeZone, request) :: dataTail ->
                match! request |> start deps timeZone with
                | Error error ->

                    do!
                        match error |> Notification.tryCreateFail request.Id errorFilter with
                        | None -> () |> async.Return
                        | Some notification -> notification |> notify

                    return! dataTail |> innerLoop (errors @ [ error ])

                | Ok result ->
                    do!

                        match result |> Notification.tryCreate errorFilter with
                        | None -> () |> async.Return
                        | Some notification -> notification |> notify

                    return result |> Ok
        }

    data |> List.ofSeq |> innerLoop []
