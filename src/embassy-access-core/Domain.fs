﻿module EmbassyAccess.Domain

open System

type City =
    | Belgrade
    | Berlin
    | Budapest
    | Sarajevo
    | Podgorica
    | Tirana
    | Paris
    | Rome
    | Dublin
    | Bern
    | Helsinki
    | Hague
    | Ljubljana

    member this.Name =
        match this with
        | Belgrade -> Constant.City.Belgrade
        | Berlin -> Constant.City.Berlin
        | Budapest -> Constant.City.Budapest
        | Sarajevo -> Constant.City.Sarajevo
        | Podgorica -> Constant.City.Podgorica
        | Tirana -> Constant.City.Tirana
        | Paris -> Constant.City.Paris
        | Rome -> Constant.City.Rome
        | Dublin -> Constant.City.Dublin
        | Bern -> Constant.City.Bern
        | Helsinki -> Constant.City.Helsinki
        | Hague -> Constant.City.Hague
        | Ljubljana -> Constant.City.Ljubljana

type Country =
    | Serbia of City
    | Germany of City
    | Bosnia of City
    | Montenegro of City
    | Albania of City
    | Hungary of City
    | Ireland of City
    | Switzerland of City
    | Finland of City
    | France of City
    | Netherlands of City
    | Slovenia of City

    member this.Name =
        match this with
        | Serbia _ -> Constant.Country.Serbia
        | Germany _ -> Constant.Country.Germany
        | Bosnia _ -> Constant.Country.Bosnia
        | Montenegro _ -> Constant.Country.Montenegro
        | Albania _ -> Constant.Country.Albania
        | Hungary _ -> Constant.Country.Hungary
        | Ireland _ -> Constant.Country.Ireland
        | Switzerland _ -> Constant.Country.Switzerland
        | Finland _ -> Constant.Country.Finland
        | France _ -> Constant.Country.France
        | Netherlands _ -> Constant.Country.Netherlands
        | Slovenia _ -> Constant.Country.Slovenia

    member this.City =
        match this with
        | Serbia city -> city
        | Germany city -> city
        | Bosnia city -> city
        | Montenegro city -> city
        | Albania city -> city
        | Hungary city -> city
        | Ireland city -> city
        | Switzerland city -> city
        | Finland city -> city
        | France city -> city
        | Netherlands city -> city
        | Slovenia city -> city

type Embassy =
    | Russian of Country
    | Spanish of Country
    | Italian of Country
    | French of Country
    | German of Country
    | British of Country

    member this.Name =
        match this with
        | Russian _ -> Constant.Embassy.Russian
        | Spanish _ -> Constant.Embassy.Spanish
        | Italian _ -> Constant.Embassy.Italian
        | French _ -> Constant.Embassy.French
        | German _ -> Constant.Embassy.German
        | British _ -> Constant.Embassy.British

    member this.Country =
        match this with
        | Russian country -> country
        | Spanish country -> country
        | Italian country -> country
        | French country -> country
        | German country -> country
        | British country -> country

type RequestId =
    | RequestId of Guid

    member this.Value =
        match this with
        | RequestId id -> id

    static member New = RequestId <| Guid.NewGuid()

type Confirmation = { Description: string }

type Appointment =
    { Value: string
      Date: DateOnly
      Time: TimeOnly
      Confirmation: Confirmation option
      Description: string option }

type ConfirmationOption =
    | FirstAvailable
    | Range of DateTime * DateTime
    | Appointment of Appointment

type RequestState =
    | Created
    | InProcess
    | Completed
    | Failed

    member this.Name =
        match this with
        | Created -> Constant.RequestState.Created
        | InProcess -> Constant.RequestState.InProcess
        | Completed -> Constant.RequestState.Completed
        | Failed -> Constant.RequestState.Failed

type Request =
    { Id: RequestId
      Value: string
      Attempt: int
      State: RequestState
      Embassy: Embassy
      Appointments: Set<Appointment>
      Description: string option
      Modified: DateTime }

module External =

    type City() =
        member val Name: string = String.Empty with get, set

    type Country() =
        member val Name: string = String.Empty with get, set
        member val City: City = City() with get, set

    type Embassy() =
        member val Name: string = String.Empty with get, set
        member val Country: Country = Country() with get, set

    and Confirmation() =
        member val Description: string = String.Empty with get, set

    and Appointment() =
        member val Value: string = String.Empty with get, set
        member val Confirmation: Confirmation option = None with get, set
        member val DateTime: DateTime = DateTime.UtcNow with get, set
        member val Description: string = String.Empty with get, set

    type Request() =
        member val Id: Guid = Guid.Empty with get, set
        member val Value: string = String.Empty with get, set
        member val Attempt: int = 0 with get, set
        member val State: string = String.Empty with get, set
        member val Embassy: Embassy = Embassy() with get, set
        member val Appointments: Appointment array = [||] with get, set
        member val Description: string = String.Empty with get, set
        member val Modified: DateTime = DateTime.UtcNow with get, set
