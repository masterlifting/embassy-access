module internal EA.Worker.Dependencies.Embassies.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Russian.Services
open EA.Russian.Services.Router
open EA.Worker.Dependencies
open EA.Worker.Dependencies.Embassies

module Kdmid =
    open EA.Russian.Services.Domain.Kdmid

    type Dependencies = {
        TaskName: string
        tryProcessFirst: Request<Payload> seq -> Async<Result<Request<Payload>, Error'>>
        getRequests: ServiceId -> Async<Result<Request<Payload> list, Error'>>
        cleanResources: unit -> Async<Result<unit, Error'>>
    } with

        static member create task cfg ct =
            let result = ResultBuilder()
            let taskName = ActiveTask.print task

            result {
                let! persistence = Persistence.Dependencies.create ()

                let! requestStorage = persistence.RussianStorage.initKdmidRequestStorage ()

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
                    |> Common.getRequests serviceId task.Duration
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

                let cleanResources () =
                    async {
                        requestStorage |> EA.Core.DataAccess.Storage.Request.dispose
                        return Ok()
                    }

                return {
                    TaskName = taskName
                    getRequests = getRequests
                    tryProcessFirst = tryProcessFirst
                    cleanResources = cleanResources
                }
            }
