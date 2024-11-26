module EA.Core.Domain

open System
open Infrastructure

module Constants =
    [<Literal>]
    let REQUESTS_STORAGE_NAME = "requests"

type CoreItem =
    { Id: Graph.NodeId
      Name: string }

    interface Graph.INodeName with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.setName name = { this with Name = name }

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

type Country =
    | Serbia of City
    | Germany of City
    | Bosnia of City
    | Montenegro of City
    | Albania of City
    | Hungary of City
    | Ireland of City
    | Italy of City
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
        | Italy city -> city
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

    static member create value =
        match value with
        | AP.IsGuid id -> RequestId id |> Ok
        | _ -> $"RequestId value: {value}" |> NotSupported |> Error

    static member New = RequestId <| Guid.NewGuid()

type AppointmentId =
    | AppointmentId of Guid

    member this.Value =
        match this with
        | AppointmentId id -> id

    static member create value =
        match value with
        | AP.IsGuid id -> AppointmentId id |> Ok
        | _ -> $"AppointmentId value: {value}" |> NotSupported |> Error

    static member New = AppointmentId <| Guid.NewGuid()


type Confirmation = { Description: string }

type Appointment =
    { Id: AppointmentId
      Value: string
      Date: DateOnly
      Time: TimeOnly
      Confirmation: Confirmation option
      Description: string }

type ConfirmationOption =
    | FirstAvailable
    | LastAvailable
    | DateTimeRange of DateTime * DateTime

type ConfirmationState =
    | Disabled
    | Manual of AppointmentId
    | Auto of ConfirmationOption

type ProcessState =
    | Created
    | InProcess
    | Completed of string
    | Failed of Error'

type Service =
    { Name: string
      Payload: string
      Embassy: Embassy
      Description: string option }

type Request =
    { Id: RequestId
      Service: Service
      Attempt: DateTime * int
      ProcessState: ProcessState
      ConfirmationState: ConfirmationState
      Appointments: Set<Appointment>
      Modified: DateTime }

type Notification =
    | Appointments of (Embassy * Set<Appointment>)
    | Confirmations of (RequestId * Embassy * Set<Confirmation>)
    | Fail of (RequestId * Error')

    static member tryCreateFail requestId allow error =
        match error |> allow with
        | true -> Fail(requestId, error) |> Some
        | false -> None

    static member tryCreate allowError request =
        match request.ProcessState with
        | Completed _ ->
            match request.Appointments.IsEmpty with
            | true -> None
            | false ->
                match request.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
                | [] -> Appointments(request.Service.Embassy, request.Appointments) |> Some
                | confirmations -> Confirmations(request.Id, request.Service.Embassy, confirmations |> set) |> Some
        | Failed error -> error |> Notification.tryCreateFail request.Id allowError
        | _ -> None

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
        member val Id: Guid = Guid.Empty with get, set
        member val Value: string = String.Empty with get, set
        member val Confirmation: Confirmation option = None with get, set
        member val DateTime: DateTime = DateTime.UtcNow with get, set
        member val Description: string = String.Empty with get, set

    type ConfirmationOption() =

        member val Type: string = String.Empty with get, set
        member val DateStart: Nullable<DateTime> = Nullable() with get, set
        member val DateEnd: Nullable<DateTime> = Nullable() with get, set

    type ConfirmationState() =

        member val Type: string = String.Empty with get, set
        member val ConfirmationOption: ConfirmationOption option = None with get, set
        member val AppointmentId: Guid option = None with get, set

    type ProcessState() =

        member val Type: string = String.Empty with get, set
        member val Error: External.Error option = None with get, set
        member val Message: string option = None with get, set

    type Service() =
        member val Name: string = String.Empty with get, set
        member val Payload: string = String.Empty with get, set
        member val Embassy: Embassy = Embassy() with get, set
        member val Description: string option = None with get, set

    type Request() =
        member val Id: Guid = Guid.Empty with get, set
        member val Service: Service = Service() with get, set
        member val Attempt: int = 0 with get, set
        member val AttemptModified: DateTime = DateTime.UtcNow with get, set
        member val ProcessState: ProcessState = ProcessState() with get, set
        member val ConfirmationState: ConfirmationState = ConfirmationState() with get, set
        member val Appointments: Appointment array = [||] with get, set
        member val Modified: DateTime = DateTime.UtcNow with get, set
