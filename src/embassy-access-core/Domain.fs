﻿module EA.Domain

open System
open Infrastructure

module Key =
    [<Literal>]
    let Requests = "requests"

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

    member this.Name = nameof this

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

    member this.Name =
        match this with
        | Serbia _ -> nameof Serbia
        | Germany _ -> nameof Germany
        | Bosnia _ -> nameof Bosnia
        | Montenegro _ -> nameof Montenegro
        | Albania _ -> nameof Albania
        | Hungary _ -> nameof Hungary
        | Ireland _ -> nameof Ireland
        | Switzerland _ -> nameof Switzerland
        | Finland _ -> nameof Finland
        | France _ -> nameof France
        | Netherlands _ -> nameof Netherlands
        | Slovenia _ -> nameof Slovenia


type Embassy =
    | Russian of Country
    | Spanish of Country
    | Italian of Country
    | French of Country
    | German of Country
    | British of Country

    member this.Country =
        match this with
        | Russian country -> country
        | Spanish country -> country
        | Italian country -> country
        | French country -> country
        | German country -> country
        | British country -> country

    member this.Name =
        match this with
        | Russian _ -> nameof Russian
        | Spanish _ -> nameof Spanish
        | Italian _ -> nameof Italian
        | French _ -> nameof French
        | German _ -> nameof German
        | British _ -> nameof British

type RequestId =
    | RequestId of Guid

    member this.Value =
        match this with
        | RequestId id -> id

    static member create value =
        match value with
        | AP.IsGuid id -> RequestId id |> Ok
        | _ -> $"RequestId value: {value}" |> NotSupported |> Error


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
    | LastAvailable
    | DateTimeRange of DateTime * DateTime

type ConfirmationState =
    | Disabled
    | Manual of Appointment
    | Auto of ConfirmationOption

type ProcessState =
    | Created
    | InProcess
    | Completed of string
    | Failed of Error'

type Request =
    { Id: RequestId
      Payload: string
      Embassy: Embassy
      Attempt: int
      ProcessState: ProcessState
      ConfirmationState: ConfirmationState
      Appointments: Set<Appointment>
      Description: string option
      GroupBy: string option
      Modified: DateTime }

type Validation = Request -> Result<unit, Error'>

type Notification =
    | Appointments of (Embassy * Set<Appointment>)
    | Confirmations of (RequestId * Embassy * Set<Confirmation>)
    | Fail of (RequestId * Error')

module External =

    type City() =
        member val Name: string = String.Empty with get, set

    type Country() =

        member val Name: string = String.Empty with get, set
        member val City: City = City() with get, set

    type Embassy() =

        member val Name: string = String.Empty with get, set
        member val Country: Country = Country() with get, set

        override this.Equals(obj: obj) : bool =
            match obj with
            | :? Embassy as x ->
                this.Name = x.Name
                && this.Country.Name = x.Country.Name
                && this.Country.City.Name = x.Country.City.Name
            | _ -> false

        override this.GetHashCode() =
            HashCode.Combine(this.Name, this.Country.Name, this.Country.City.Name)

    type Confirmation() =
        member val Description: string = String.Empty with get, set

    type Appointment() =
        member val Value: string = String.Empty with get, set
        member val Confirmation: Confirmation option = None with get, set
        member val DateTime: DateTime = DateTime.UtcNow with get, set
        member val Description: string option = None with get, set

    type ConfirmationOption() =

        member val Type: string = String.Empty with get, set
        member val DateStart: Nullable<DateTime> = Nullable() with get, set
        member val DateEnd: Nullable<DateTime> = Nullable() with get, set

    type ConfirmationState() =

        member val Type: string = String.Empty with get, set
        member val Option: ConfirmationOption option = None with get, set
        member val Appointment: Appointment option = None with get, set

    type RequestState() =

        member val Type: string = String.Empty with get, set
        member val Error: External.Error option = None with get, set
        member val Message: string option = None with get, set


    type Request() =
        member val Id: Guid = Guid.Empty with get, set
        member val Payload: string = String.Empty with get, set
        member val Embassy: Embassy = Embassy() with get, set

        member val State: RequestState =
            let state = RequestState()
            state.Type <- nameof Created
            state with get, set

        member val Attempt: int = 0 with get, set

        member val ConfirmationState: ConfirmationState =
            let state = ConfirmationState()
            state.Type <- nameof Disabled
            state with get, set

        member val Appointments: Appointment array = [||] with get, set
        member val Description: string option = None with get, set
        member val GroupBy: string option = None with get, set
        member val Modified: DateTime = DateTime.UtcNow with get, set
