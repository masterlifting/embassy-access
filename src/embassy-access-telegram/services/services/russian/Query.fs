module EA.Telegram.Services.Services.Russian.Query

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Router.Services
open EA.Telegram.Dependencies.Services.Russian
open EA.Russian.Services.Router

let private (|Kdmid|Midpass|ServiceNotFound|) (serviceId: ServiceId) =
    match serviceId |> parse with
    | Ok route ->
        match route with
        | Passport route ->
            match route with
            | Passport.Status -> Midpass
            | Passport.PassportFiveYears ops -> Kdmid ops
            | Passport.BiometricPassport ops -> Kdmid ops
            | Passport.PassportIssuance ops -> Kdmid ops        
        | Notary route ->
            match route with
            | Notary.MarriageCertificateSerbia ops -> Kdmid ops
            | Notary.MarriageCertificate ops -> Kdmid ops
            | Notary.DivorceCertificate ops -> Kdmid ops
            | Notary.NameChange ops -> Kdmid ops
            | Notary.ElectronicDocVerification ops -> Kdmid ops
            | Notary.RegistrationRemoval ops -> Kdmid ops
            | Notary.ConsularRegistration ops -> Kdmid ops
            | Notary.MarriageRegistration ops -> Kdmid ops
            | Notary.WillCertification ops -> Kdmid ops
            | Notary.DocumentClaim ops -> Kdmid ops
            | Notary.PowerOfAttorneyCertification ops -> Kdmid ops
            | Notary.CopyCertification ops -> Kdmid ops
            | Notary.TranslationCertification ops -> Kdmid ops
            | Notary.ConsentCertification ops -> Kdmid ops
            | Notary.CertificateIssuance ops -> Kdmid ops
        | Citizenship route ->
            match route with
            | Citizenship.Renunciation ops -> Kdmid ops
            | Citizenship.ChildCitizenshipBothParents ops -> Kdmid ops
            | Citizenship.ChildCitizenshipMixedMarriage ops -> Kdmid ops
            | Citizenship.CitizenshipTermination ops -> Kdmid ops
            | Citizenship.CitizenshipVerification ops -> Kdmid ops
        | Pension route ->
            match route with
            | Pension.InitialPensionConsultation ops -> Kdmid ops
            | Pension.OtherPensionMatters ops -> Kdmid ops
            | Pension.PensionFundCertificate ops -> Kdmid ops
    | Error error -> ServiceNotFound error

let getService embassyId serviceId forUser =
    fun (deps: Russian.Dependencies) ->
        match serviceId with
        | Kdmid op ->
            deps
            |> Kdmid.Dependencies.create
            |> Kdmid.Query.getService op serviceId embassyId forUser
        | Midpass ->
            deps
            |> Midpass.Dependencies.create
            |> Midpass.Query.getService serviceId embassyId forUser
        | ServiceNotFound _ ->
            $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return
