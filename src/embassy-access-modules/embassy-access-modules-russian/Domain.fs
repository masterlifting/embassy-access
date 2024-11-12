module EA.Embassies.Russian.Domain

open System
open Infrastructure
open EA.Core.Domain

type Service =
    { Country: Country
      Payload: string
      Confirmation: ConfirmationState }

type PassportServices =
    | IssueForeignPassport of Service
    | CheckPassportStatus of Service
    | TakeReadyPassport of Service

    member this.Name =
        match this with
        | IssueForeignPassport _ -> "Подача заявления на оформление загранпаспорта"
        | CheckPassportStatus _ -> "Проверка статуса готовности загранпаспорта"
        | TakeReadyPassport _ -> "Получение готового загранпаспорта"

type NotaryServices =
    | CertPowerAttorney of Service

    member this.Name =
        match this with
        | CertPowerAttorney _ -> "Оформление доверенности"

type CitizenshipServices =
    | RenunciationOfCitizenship of Service

    member this.Name =
        match this with
        | RenunciationOfCitizenship _ -> "Оформление отказа от гражданства"

type Services =
    | Passport of PassportServices
    | Notary of NotaryServices
    | Citizenship of CitizenshipServices

    member this.Name =
        match this with
        | Passport _ -> "Паспорт"
        | Notary _ -> "Нотариат"
        | Citizenship _ -> "Гражданство"

    member this.createRequest() =

        let service =
            match this with
            | Passport service ->
                match service with
                | IssueForeignPassport service -> service
                | CheckPassportStatus service -> service
                | TakeReadyPassport service -> service
            | Notary service ->
                match service with
                | CertPowerAttorney service -> service
            | Citizenship service ->
                match service with
                | RenunciationOfCitizenship service -> service

        { Id = RequestId.New
          Name = this.Name
          Payload = service.Payload
          Embassy = Russian service.Country
          ProcessState = Created
          Attempt = (DateTime.UtcNow, 0)
          ConfirmationState = service.Confirmation
          Appointments = Set.empty
          Description = None
          GroupBy = Some this.Name
          Modified = DateTime.UtcNow }

module ErrorCodes =

    [<Literal>]
    let PAGE_HAS_ERROR = "PageHasError"

    [<Literal>]
    let NOT_CONFIRMED = "NotConfirmed"

    [<Literal>]
    let CONFIRMATIONS_EXISTS = "ConfirmationExists"

    [<Literal>]
    let REQUEST_DELETED = "RequestDeleted"

type ProcessRequestConfiguration = { TimeShift: int8 }

type StorageUpdateRequest = EA.Core.Domain.Request -> Async<Result<EA.Core.Domain.Request, Error'>>

type HttpGetStringRequest =
    Web.Http.Domain.Request -> Web.Http.Domain.Client -> Async<Result<Web.Http.Domain.Response<string>, Error'>>

type HttpGetBytesRequest =
    Web.Http.Domain.Request -> Web.Http.Domain.Client -> Async<Result<Web.Http.Domain.Response<byte array>, Error'>>

type HttpPostStringRequest =
    Web.Http.Domain.Request -> Web.Http.Domain.RequestContent -> Web.Http.Domain.Client -> Async<Result<string, Error'>>

type SolveCaptchaImage = byte array -> Async<Result<int, Error'>>

type ProcessRequestDeps =
    { Configuration: ProcessRequestConfiguration
      updateRequest: StorageUpdateRequest
      getInitialPage: HttpGetStringRequest
      getCaptcha: HttpGetBytesRequest
      solveCaptcha: SolveCaptchaImage
      postValidationPage: HttpPostStringRequest
      postAppointmentsPage: HttpPostStringRequest
      postConfirmationPage: HttpPostStringRequest }

type Id = private Id of int
type Cd = private Cd of string
type Ems = private Ems of string option

type Credentials =
    { City: EA.Core.Domain.City
      Id: Id
      Cd: Cd
      Ems: Ems }

    member this.Value =
        match this with
        | { City = city
            Id = Id id
            Cd = Cd cd
            Ems = Ems ems } ->
            match city with
            | EA.Core.Domain.Belgrade -> ("belgrad", id, cd, ems)
            | EA.Core.Domain.Budapest -> ("budapest", id, cd, ems)
            | EA.Core.Domain.Sarajevo -> ("sarajevo", id, cd, ems)
            | EA.Core.Domain.Podgorica -> ("podgorica", id, cd, ems)
            | EA.Core.Domain.Tirana -> ("tirana", id, cd, ems)
            | EA.Core.Domain.Paris -> ("paris", id, cd, ems)
            | EA.Core.Domain.Rome -> ("rome", id, cd, ems)
            | EA.Core.Domain.Berlin -> ("berlin", id, cd, ems)
            | EA.Core.Domain.Dublin -> ("dublin", id, cd, ems)
            | EA.Core.Domain.Bern -> ("bern", id, cd, ems)
            | EA.Core.Domain.Helsinki -> ("helsinki", id, cd, ems)
            | EA.Core.Domain.Hague -> ("hague", id, cd, ems)
            | EA.Core.Domain.Ljubljana -> ("ljubljana", id, cd, ems)

let createCredentials url =
    url
    |> Web.Http.Client.Route.toUri
    |> Result.bind (fun uri ->
        match uri.Host.Split '.' with
        | hostParts when hostParts.Length < 3 -> Error <| NotSupported $"Kdmid. City in {url}."
        | hostParts ->
            uri
            |> Web.Http.Client.Route.toQueryParams
            |> Result.bind (fun paramsMap ->
                let city =
                    match hostParts[0] with
                    | "belgrad" -> Ok EA.Core.Domain.Belgrade
                    | "budapest" -> Ok EA.Core.Domain.Budapest
                    | "sarajevo" -> Ok EA.Core.Domain.Sarajevo
                    | "berlin" -> Ok EA.Core.Domain.Berlin
                    | "podgorica" -> Ok EA.Core.Domain.Podgorica
                    | "tirana" -> Ok EA.Core.Domain.Tirana
                    | "paris" -> Ok EA.Core.Domain.Paris
                    | "rome" -> Ok EA.Core.Domain.Rome
                    | "dublin" -> Ok EA.Core.Domain.Dublin
                    | "bern" -> Ok EA.Core.Domain.Bern
                    | "helsinki" -> Ok EA.Core.Domain.Helsinki
                    | "hague" -> Ok EA.Core.Domain.Hague
                    | "ljubljana" -> Ok EA.Core.Domain.Ljubljana
                    | _ -> Error $"City {hostParts[0]} is not supported"

                let id =
                    paramsMap
                    |> Map.tryFind "id"
                    |> Option.map (function
                        | AP.IsInt id when id > 1000 -> Ok <| Id id
                        | _ -> Error $"Invalid id parameter ")
                    |> Option.defaultValue (Error $"Id parameter is missing")

                let cd =
                    paramsMap
                    |> Map.tryFind "cd"
                    |> Option.map (function
                        | AP.IsLettersOrNumbers cd -> Ok <| Cd cd
                        | _ -> Error $"Invalid cd parameter")
                    |> Option.defaultValue (Error $"Cd parameter is missing")

                let ems =
                    paramsMap
                    |> Map.tryFind "ems"
                    |> Option.map (function
                        | AP.IsLettersOrNumbers ems -> Ok <| Ems(Some ems)
                        | _ -> Error $"Invalid ems parameter")
                    |> Option.defaultValue (Ok <| Ems None)

                match city, id, cd, ems with
                | Ok city, Ok id, Ok cd, Ok ems ->
                    let credentials: Credentials =
                        { City = city
                          Id = id
                          Cd = cd
                          Ems = ems }

                    Ok credentials
                | _ ->
                    let errors =
                        let error =
                            function
                            | Error error -> Some error
                            | _ -> None

                        [ error city; error id; error cd; error ems ]
                        |> List.choose Operators.id
                        |> List.fold (fun acc error -> $"{acc},{error}") ""

                    Error <| NotSupported $"Parameters in {url}. {errors}."))
