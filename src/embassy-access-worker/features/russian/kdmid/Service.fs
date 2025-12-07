module internal EA.Worker.Features.Russian.Kdmid.Service

open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Russian.Services
open EA.Russian.Services.Router
open EA.Russian.Services.Domain.Kdmid
open EA.Worker.Shared
open EA.Worker.Features.Russian.Kdmid.Infra

type private Dependencies = {
    TaskName: string
    tryProcessFirst: Request<Payload> seq -> Async<Result<Request<Payload>, Error'>>
    getRequests: ServiceId -> Async<Result<Request<Payload> list, Error'>>
    cleanupResources: unit option -> Result<unit, Error'>
} with

    static member create task (deps: Worker.Task.Dependencies) ct =
        let result = ResultBuilder()
        let taskName = ActiveTask.print task

        result {
            let! requestStorage = RequestStorage.init deps.Persistence.ConnectionString

            let rec handleProcessResult (result: Result<Request<Payload>, Error'>) =
                result
                |> ResultAsync.wrap (fun r -> Ok() |> async.Return) //TODO: add result handling
                |> ResultAsync.mapError (fun error -> taskName + error.Message |> Log.crt)
                |> Async.Ignore

            let hasRequiredService serviceId =
                let isRequiredService =
                    function
                    | Passport Passport.Status -> false
                    | Passport(Passport.PassportFiveYears op | Passport.BiometricPassport op | Passport.PassportIssuance op)
                    | Notary(Notary.MarriageCertificate op | Notary.MarriageCertificateAssurance op | Notary.DivorceCertificate op | Notary.NameChange op | Notary.ElectronicDocVerification op | Notary.RegistrationRemoval op | Notary.ConsularRegistration op | Notary.MarriageRegistration op | Notary.WillCertification op | Notary.DocumentClaim op | Notary.PowerOfAttorneyCertification op | Notary.CopyCertification op | Notary.TranslationCertification op | Notary.ConsentCertification op | Notary.SignatureCertification op | Notary.CertificateIssuance op)
                    | Citizenship(Citizenship.ChildCitizenshipBothParents op | Citizenship.ChildCitizenshipMixedMarriage op | Citizenship.CitizenshipTermination op | Citizenship.CitizenshipVerification op)
                    | Pension(Pension.InitialPensionConsultation op | Pension.OtherPensionMatters op | Pension.PensionFundCertificate op) ->
                        match op with
                        | Kdmid.Operation.AutoNotifications
                        | Kdmid.Operation.AutoBookingFirst
                        | Kdmid.Operation.AutoBookingFirstInPeriod
                        | Kdmid.Operation.AutoBookingLast -> true
                        | Kdmid.Operation.ManualRequest -> false

                serviceId |> Router.parse |> Result.exists isRequiredService

            let getRequests serviceId =
                (requestStorage, hasRequiredService)
                |> Embassies.getRequests serviceId task.Duration
                |> ResultAsync.map (
                    List.filter (fun request ->
                        match request.Payload.State with
                        | NoAppointments
                        | HasAppointments _ -> true
                        | HasConfirmation _ -> false)
                )

            let tryProcessFirst requests =

                Kdmid.Client.init {
                    ct = ct
                    AntiCaptchaApiKey = Configuration.ENVIRONMENTS.AntiCaptchaKey
                    RequestStorage = requestStorage
                }
                |> Result.map (fun client -> client, handleProcessResult)
                |> ResultAsync.wrap (Kdmid.Service.tryProcessFirst requests)

            let cleanupResources _ =
                requestStorage |> RequestStorage.dispose

            return {
                TaskName = taskName
                getRequests = getRequests
                tryProcessFirst = tryProcessFirst
                cleanupResources = cleanupResources
            }
        }

let private processGroup requests =
    fun (deps: Dependencies) ->
        deps.tryProcessFirst requests
        |> ResultAsync.map (fun request ->
            match request.Payload.State with
            | NoAppointments -> deps.TaskName + " No appointments found." |> Log.dbg
            | HasAppointments appointments -> deps.TaskName + $" Appointments found: %i{appointments.Count}" |> Log.scs
            | HasConfirmation(msg, appointment) ->
                deps.TaskName
                + $" Confirmation found: %s{msg}. %s{Appointment.print appointment}"
                |> Log.scs)

let private start =
    fun (deps: Dependencies) ->
        let inline processGroup requests = deps |> processGroup requests

        [ Services.ROOT_ID; Embassies.RUS ]
        |> ServiceId.combine
        |> deps.getRequests
        |> ResultAsync.map (fun requests ->
            requests
            |> Seq.groupBy _.Service.Id
            |> Seq.map (fun (_, requests) ->
                requests
                |> Seq.sortByDescending _.Modified
                |> Seq.truncate 5
                |> Seq.toList
                |> processGroup))
        |> ResultAsync.map (Async.Sequential >> Async.map Result.unzip)
        |> Async.bind (function
            | Error error -> Error error |> async.Return
            | Ok results ->
                results
                |> Async.map (fun (_, errors) ->
                    errors |> Seq.iter (fun error -> deps.TaskName + error.Message |> Log.crt) |> Ok))
        |> ResultAsync.apply deps.cleanupResources

let searchAppointments (task, deps, ct) =
    Dependencies.create task deps ct |> ResultAsync.wrap start
