module EA.Embassies.Russian.Domain

open EA.Embassies.Russian

type PassportServices =
    | IssueForeign of (Kdmid.Domain.Dependencies * Kdmid.Domain.Request)
    | CheckReadiness of Midpass.Domain.Request

    member this.Name =
        match this with
        | IssueForeign _ -> "Проверка записи на прием"
        | CheckReadiness _ -> "Проверка готовности"

type NotaryServices =
    | PowerOfAttorney of (Kdmid.Domain.Dependencies * Kdmid.Domain.Request)

    member this.Name =
        match this with
        | PowerOfAttorney _ -> "Доверенность.Проверка записи на прием"

type CitizenshipServices =
    | CitizenshipRenunciation of (Kdmid.Domain.Dependencies * Kdmid.Domain.Request)

    member this.Name =
        match this with
        | CitizenshipRenunciation _ -> "Отказ.Проверка записи на прием"

type Services =
    | Passport of PassportServices
    | Notary of NotaryServices
    | Citizenship of CitizenshipServices

    member this.Name =
        match this with
        | Passport service -> "Паспорт." + service.Name
        | Notary service -> "Нотариат." + service.Name
        | Citizenship service -> "Гражданство." + service.Name

    member this.createRequest() =
        match this with
        | Passport service ->
            match service with
            | IssueForeign (_, request) -> request.Create this.Name
            | CheckReadiness request -> request.Create this.Name
        | Notary service ->
            match service with
            | PowerOfAttorney (_, request) -> request.Create this.Name
        | Citizenship service ->
            match service with
            | CitizenshipRenunciation (_, request) -> request.Create this.Name
