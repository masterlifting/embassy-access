module EA.Core.Domain

open System
open Infrastructure

module Constants =

    module Embassy =
        [<Literal>]
        let RUSSIAN = "Russian"

        [<Literal>]
        let SPANISH = "Spanish"

        [<Literal>]
        let ITALIAN = "Italian"

        [<Literal>]
        let GERMAN = "German"

        [<Literal>]
        let FRENCH = "French"

        [<Literal>]
        let BRITISH = "British"

    module Country =

        [<Literal>]
        let SERBIA = "Serbia"

        [<Literal>]
        let BOSNIA = "Bosnia"

        [<Literal>]
        let MONTENEGRO = "Montenegro"

        [<Literal>]
        let ALBANIA = "Albania"

        [<Literal>]
        let HUNGARY = "Hungary"

        [<Literal>]
        let SLOVENIA = "Slovenia"

        [<Literal>]
        let SWITZERLAND = "Switzerland"

        [<Literal>]
        let NETHERLANDS = "Netherlands"

        [<Literal>]
        let ITALY = "Italy"

        [<Literal>]
        let FRANCE = "France"

        [<Literal>]
        let GERMANY = "Germany"

        [<Literal>]
        let IRELAND = "Ireland"

        [<Literal>]
        let FINLAND = "Finland"

    module City =

        [<Literal>]
        let BELGRADE = "Belgrade"

        [<Literal>]
        let SARAJEVO = "Sarajevo"

        [<Literal>]
        let PODGORICA = "Podgorica"

        [<Literal>]
        let TIRANA = "Tirana"

        [<Literal>]
        let BUDAPEST = "Budapest"

        [<Literal>]
        let LJUBLJANA = "Ljubljana"

        [<Literal>]
        let BERN = "Bern"

        [<Literal>]
        let HAGUE = "Hague"

        [<Literal>]
        let ROME = "Rome"

        [<Literal>]
        let PARIS = "Paris"

        [<Literal>]
        let BERLIN = "Berlin"

        [<Literal>]
        let DUBLIN = "Dublin"

        [<Literal>]
        let HELSINKI = "Helsinki"

type Embassy =
    { Id: Graph.NodeId
      Name: string
      Description: string option }

    interface Graph.INodeName with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.set(id, name) = { this with Id = id; Name = name }

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

    type Graph() =
        member val Id: string = String.Empty with get, set
        member val Name: string = String.Empty with get, set
        member val Description: string option = None with get, set
        member val Children: Graph[] = [||] with get, set