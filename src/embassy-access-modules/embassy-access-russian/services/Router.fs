module EA.Russian.Services.Router

open EA.Core.Domain
open Infrastructure.Domain

module Kdmid =
    module Operation =
        type Route =
            | ManualRequest
            | AutoNotifications
            | AutoBookingFirst
            | AutoBookingFirstInPeriod
            | AutoBookingLast

            member this.Value =
                match this with
                | ManualRequest -> "0"
                | AutoNotifications -> "1"
                | AutoBookingFirst -> "2"
                | AutoBookingFirstInPeriod -> "3"
                | AutoBookingLast -> "4"

        let parse (input: string) =
            match input with
            | "0" -> ManualRequest |> Ok
            | "1" -> AutoNotifications |> Ok
            | "2" -> AutoBookingFirst |> Ok
            | "3" -> AutoBookingFirstInPeriod |> Ok
            | "4" -> AutoBookingLast |> Ok
            | _ ->
                "Service operation for the Russian embassy is not supported."
                |> NotSupported
                |> Error

module Passport =

    type Route =
        | Status
        | PassportFiveYears of Kdmid.Operation.Route
        | BiometricPassport of Kdmid.Operation.Route
        | PassportIssuance of Kdmid.Operation.Route

        member this.Value =
            match this with
            | Status -> [ "0" ]
            | PassportFiveYears op -> [ "1"; op.Value ]
            | BiometricPassport op -> [ "2"; op.Value ]
            | PassportIssuance op -> [ "3"; op.Value ]

    let parse (input: string list) =
        match input with
        | [ "0" ] -> Status |> Ok
        | [ "1"; op ] -> op |> Kdmid.Operation.parse |> Result.map PassportFiveYears
        | [ "2"; op ] -> op |> Kdmid.Operation.parse |> Result.map BiometricPassport
        | [ "3"; op ] -> op |> Kdmid.Operation.parse |> Result.map PassportIssuance
        | _ ->
            "Passport service for the Russian embassy is not supported."
            |> NotSupported
            |> Error

module Notary =

    type Route =
        | MarriageCertificate of Kdmid.Operation.Route
        | MarriageCertificateAssurance of Kdmid.Operation.Route
        | DivorceCertificate of Kdmid.Operation.Route
        | NameChange of Kdmid.Operation.Route
        | ElectronicDocVerification of Kdmid.Operation.Route
        | RegistrationRemoval of Kdmid.Operation.Route
        | ConsularRegistration of Kdmid.Operation.Route
        | MarriageRegistration of Kdmid.Operation.Route
        | WillCertification of Kdmid.Operation.Route
        | DocumentClaim of Kdmid.Operation.Route
        | PowerOfAttorneyCertification of Kdmid.Operation.Route
        | CopyCertification of Kdmid.Operation.Route
        | TranslationCertification of Kdmid.Operation.Route
        | ConsentCertification of Kdmid.Operation.Route
        | SignatureCertification of Kdmid.Operation.Route
        | CertificateIssuance of Kdmid.Operation.Route

        member this.Value =
            match this with
            | MarriageCertificate op -> [ "0"; op.Value ]
            | MarriageCertificateAssurance op -> [ "1"; op.Value ]
            | DivorceCertificate op -> [ "2"; op.Value ]
            | NameChange op -> [ "3"; op.Value ]
            | ElectronicDocVerification op -> [ "4"; op.Value ]
            | RegistrationRemoval op -> [ "5"; op.Value ]
            | ConsularRegistration op -> [ "6"; op.Value ]
            | MarriageRegistration op -> [ "7"; op.Value ]
            | WillCertification op -> [ "8"; op.Value ]
            | DocumentClaim op -> [ "9"; op.Value ]
            | PowerOfAttorneyCertification op -> [ "10"; op.Value ]
            | CopyCertification op -> [ "11"; op.Value ]
            | TranslationCertification op -> [ "12"; op.Value ]
            | ConsentCertification op -> [ "13"; op.Value ]
            | SignatureCertification op -> [ "14"; op.Value ]
            | CertificateIssuance op -> [ "15"; op.Value ]

    let parse (input: string list) =
        match input with
        | [ "0"; op ] -> op |> Kdmid.Operation.parse |> Result.map MarriageCertificate
        | [ "1"; op ] -> op |> Kdmid.Operation.parse |> Result.map MarriageCertificateAssurance
        | [ "2"; op ] -> op |> Kdmid.Operation.parse |> Result.map DivorceCertificate
        | [ "3"; op ] -> op |> Kdmid.Operation.parse |> Result.map NameChange
        | [ "4"; op ] -> op |> Kdmid.Operation.parse |> Result.map ElectronicDocVerification
        | [ "5"; op ] -> op |> Kdmid.Operation.parse |> Result.map RegistrationRemoval
        | [ "6"; op ] -> op |> Kdmid.Operation.parse |> Result.map ConsularRegistration
        | [ "7"; op ] -> op |> Kdmid.Operation.parse |> Result.map MarriageRegistration
        | [ "8"; op ] -> op |> Kdmid.Operation.parse |> Result.map WillCertification
        | [ "9"; op ] -> op |> Kdmid.Operation.parse |> Result.map DocumentClaim
        | [ "10"; op ] -> op |> Kdmid.Operation.parse |> Result.map PowerOfAttorneyCertification
        | [ "11"; op ] -> op |> Kdmid.Operation.parse |> Result.map CopyCertification
        | [ "12"; op ] -> op |> Kdmid.Operation.parse |> Result.map TranslationCertification
        | [ "13"; op ] -> op |> Kdmid.Operation.parse |> Result.map ConsentCertification
        | [ "14"; op ] -> op |> Kdmid.Operation.parse |> Result.map SignatureCertification
        | [ "15"; op ] -> op |> Kdmid.Operation.parse |> Result.map CertificateIssuance
        | _ ->
            "Notary service for the Russian embassy is not supported."
            |> NotSupported
            |> Error

module Citizenship =

    type Route =
        | ChildCitizenshipBothParents of Kdmid.Operation.Route
        | ChildCitizenshipMixedMarriage of Kdmid.Operation.Route
        | CitizenshipTermination of Kdmid.Operation.Route
        | CitizenshipVerification of Kdmid.Operation.Route

        member this.Value =
            match this with
            | ChildCitizenshipBothParents op -> [ "0"; op.Value ]
            | ChildCitizenshipMixedMarriage op -> [ "1"; op.Value ]
            | CitizenshipTermination op -> [ "2"; op.Value ]
            | CitizenshipVerification op -> [ "3"; op.Value ]

    let parse (input: string list) =
        match input with
        | [ "0"; op ] -> op |> Kdmid.Operation.parse |> Result.map ChildCitizenshipBothParents
        | [ "1"; op ] -> op |> Kdmid.Operation.parse |> Result.map ChildCitizenshipMixedMarriage
        | [ "2"; op ] -> op |> Kdmid.Operation.parse |> Result.map CitizenshipTermination
        | [ "3"; op ] -> op |> Kdmid.Operation.parse |> Result.map CitizenshipVerification
        | _ ->
            "Citizenship service for the Russian embassy is not supported."
            |> NotSupported
            |> Error

module Pension =

    type Route =
        | InitialPensionConsultation of Kdmid.Operation.Route
        | OtherPensionMatters of Kdmid.Operation.Route
        | PensionFundCertificate of Kdmid.Operation.Route

        member this.Value =
            match this with
            | InitialPensionConsultation op -> [ "0"; op.Value ]
            | OtherPensionMatters op -> [ "1"; op.Value ]
            | PensionFundCertificate op -> [ "2"; op.Value ]

    let parse (input: string list) =
        match input with
        | [ "0"; op ] -> op |> Kdmid.Operation.parse |> Result.map InitialPensionConsultation
        | [ "1"; op ] -> op |> Kdmid.Operation.parse |> Result.map OtherPensionMatters
        | [ "2"; op ] -> op |> Kdmid.Operation.parse |> Result.map PensionFundCertificate
        | _ ->
            "Pension service for the Russian embassy is not supported."
            |> NotSupported
            |> Error

type Route =
    | Passport of Passport.Route
    | Notary of Notary.Route
    | Citizenship of Citizenship.Route
    | Pension of Pension.Route

    member this.Value =
        match this with
        | Passport r -> "0" :: r.Value
        | Notary r -> "1" :: r.Value
        | Citizenship r -> "2" :: r.Value
        | Pension r -> "3" :: r.Value

let parse (serviceId: ServiceId) =
    // Maybe I should make sure that the serviceId is an Russian serviceId
    let input = serviceId.Value |> Tree.NodeId.splitValues |> List.skip 2
    let remaining = input[1..]

    match input[0] with
    | "0" -> remaining |> Passport.parse |> Result.map Passport
    | "1" -> remaining |> Notary.parse |> Result.map Notary
    | "2" -> remaining |> Citizenship.parse |> Result.map Citizenship
    | "3" -> remaining |> Pension.parse |> Result.map Pension
    | _ ->
        $"'%s{serviceId.ValueStr}' for the Russian embassy is not supported."
        |> NotSupported
        |> Error
