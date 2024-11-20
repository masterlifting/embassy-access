module EA.Embassies.Russian.Domain

open EA.Embassies.Russian

type PassportServices =
    | IssueForeign of (Kdmid.Domain.Dependencies * Kdmid.Domain.ServiceRequest)
    | CheckReadiness of Midpass.Domain.Request

    static member private ISSUE_FOREIGN = "Паспорт.Проверка записи на прием"
    static member private CHECK_READINESS = "Паспорт.Проверка готовности"

    member this.Name =
        match this with
        | IssueForeign _ -> PassportServices.ISSUE_FOREIGN
        | CheckReadiness _ -> PassportServices.CHECK_READINESS

    static member LIST = [ PassportServices.ISSUE_FOREIGN; PassportServices.CHECK_READINESS ]

type NotaryServices =
    | PowerOfAttorney of (Kdmid.Domain.Dependencies * Kdmid.Domain.ServiceRequest)

    static member private POWER_OF_ATTORNEY = "Доверенность.Проверка записи на прием"

    member this.Name =
        match this with
        | PowerOfAttorney _ -> NotaryServices.POWER_OF_ATTORNEY

    static member LIST = [ NotaryServices.POWER_OF_ATTORNEY ]

type CitizenshipServices =
    | CitizenshipRenunciation of (Kdmid.Domain.Dependencies * Kdmid.Domain.ServiceRequest)

    static member private RENUNCIATION = "Отказ.Проверка записи на прием"

    member this.Name =
        match this with
        | CitizenshipRenunciation _ -> CitizenshipServices.RENUNCIATION

    static member LIST = [ CitizenshipServices.RENUNCIATION ]

type Service =
    | Passport of PassportServices
    | Notary of NotaryServices
    | Citizenship of CitizenshipServices

    static member private PASSPORT = "Паспорт"
    static member private NOTARY = "Нотариат"
    static member private CITIZENSHIP = "Гражданство"

    member this.Name =
        match this with
        | Passport service -> Service.PASSPORT + "." + service.Name
        | Notary service -> Service.NOTARY + "." + service.Name
        | Citizenship service -> Service.CITIZENSHIP + "." + service.Name

    static member LIST = [ Service.PASSPORT; Service.NOTARY; Service.CITIZENSHIP ]

    member this.createRequest() =
        match this with
        | Passport service ->
            match service with
            | IssueForeign(_, request) -> request.CreateRequest this.Name
            | CheckReadiness request -> request.Create this.Name
        | Notary service ->
            match service with
            | PowerOfAttorney(_, request) -> request.CreateRequest this.Name
        | Citizenship service ->
            match service with
            | CitizenshipRenunciation(_, request) -> request.CreateRequest this.Name
