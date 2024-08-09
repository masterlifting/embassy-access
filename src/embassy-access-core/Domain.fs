module EmbassyAccess.Domain

open System
open EmbassyAccess.Constants

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
        | Belgrade -> City.Belgrade
        | Berlin -> City.Berlin
        | Budapest -> City.Budapest
        | Sarajevo -> City.Sarajevo
        | Podgorica -> City.Podgorica
        | Tirana -> City.Tirana
        | Paris -> City.Paris
        | Rome -> City.Rome
        | Dublin -> City.Dublin
        | Bern -> City.Bern
        | Helsinki -> City.Helsinki
        | Hague -> City.Hague
        | Ljubljana -> City.Ljubljana

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
        | Serbia _ -> Country.Serbia
        | Germany _ -> Country.Germany
        | Bosnia _ -> Country.Bosnia
        | Montenegro _ -> Country.Montenegro
        | Albania _ -> Country.Albania
        | Hungary _ -> Country.Hungary
        | Ireland _ -> Country.Ireland
        | Switzerland _ -> Country.Switzerland
        | Finland _ -> Country.Finland
        | France _ -> Country.France
        | Netherlands _ -> Country.Netherlands
        | Slovenia _ -> Country.Slovenia

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
        | Russian _ -> Embassy.Russian
        | Spanish _ -> Embassy.Spanish
        | Italian _ -> Embassy.Italian
        | French _ -> Embassy.French
        | German _ -> Embassy.German
        | British _ -> Embassy.British

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
    | Running
    | Completed
    | Failed

    member this.Name =
        match this with
        | Created -> RequestState.Created
        | Running -> RequestState.Running
        | Completed -> RequestState.Completed
        | Failed -> RequestState.Failed

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
