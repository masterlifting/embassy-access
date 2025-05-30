﻿module internal EA.Worker.Dependencies.Embassies.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Russian.Services
open EA.Russian.Services.Router
open EA.Telegram.DataAccess
open EA.Telegram.Services.Services.Russian
open EA.Worker.Dependencies
open EA.Worker.Dependencies.Embassies

module Kdmid =
    open EA.Russian.Services.Domain.Kdmid

    type Dependencies = {
        TaskName: string
        tryProcessFirst: Request<Payload> seq -> Async<Result<Request<Payload>, Error'>>
        getRequests: ServiceId -> Async<Result<Request<Payload> list, Error'>>
    } with

        static member create (task: ActiveTask) cfg ct =
            let result = ResultBuilder()
            let taskName = ActiveTask.print task + " "

            result {
                let! persistence = Persistence.Dependencies.create cfg
                let! telegram = Telegram.Dependencies.create cfg ct

                let! chatStorage = persistence.initChatStorage ()
                let! requestStorage = persistence.RussianStorage.initKdmidRequestStorage ()

                let getChats subscriptions =
                    chatStorage |> Storage.Chat.Query.findManyBySubscriptions subscriptions

                let getRequests embassyIs serviceId =
                    requestStorage |> Common.getRequests embassyIs serviceId

                let updateRequests requests =
                    requestStorage |> Storage.Request.Command.updateSeq requests

                let spreadTranslatedMessages data =
                    (telegram.Culture.translateSeq, telegram.Web.Telegram.sendMessages)
                    |> Common.spreadTranslatedMessages data

                let processAll requests =
                    fun handleProcessResult ->

                        Kdmid.Client.init {
                            ct = ct
                            RequestStorage = requestStorage
                        }
                        |> Result.map (fun client -> client, handleProcessResult)
                        |> ResultAsync.wrap (Kdmid.Service.tryProcessAll requests)

                let rec handleProcessResult (result: Result<Request<Payload>, Error'>) =
                    result
                    |> ResultAsync.wrap (fun r ->
                        Kdmid.Command.handleProcessResult r {
                            TaskName = taskName
                            getChats = getChats
                            getRequests = getRequests
                            updateRequests = updateRequests
                            processAllRequests = fun requests -> processAll requests handleProcessResult
                            spreadTranslatedMessages = spreadTranslatedMessages
                        })
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

                let getRequestsToProcess rootServiceId =
                    (requestStorage, hasRequiredService)
                    |> Common.getRequestsToProcess rootServiceId task.Duration
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
                        RequestStorage = requestStorage
                    }
                    |> Result.map (fun client -> client, handleProcessResult)
                    |> ResultAsync.wrap (Kdmid.Service.tryProcessFirst requests)

                return {
                    TaskName = taskName
                    getRequests = getRequestsToProcess
                    tryProcessFirst = tryProcessFirst
                }
            }
