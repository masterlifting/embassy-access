[<RequireQualifiedAccess>]
module EA.Core.DataAccess.Request

open System
open Infrastructure
open EA.Core.Domain
open Persistence.Domain

[<Literal>]
let private Name = "Requests"

type RequestStorage = RequestStorage of Storage

type StorageType =
    | InMemory
    | FileSystem of filepath: string

type Confirmation() =
    member val Description: string = String.Empty with get, set

    member this.ToDomain() = { Description = this.Description }

type Appointment() =
    member val Id: Guid = Guid.Empty with get, set
    member val Value: string = String.Empty with get, set
    member val Confirmation: Confirmation option = None with get, set
    member val DateTime: DateTime = DateTime.UtcNow with get, set
    member val Description: string = String.Empty with get, set

    member this.ToDomain() =
        { Id = this.Id |> AppointmentId
          Value = this.Value
          Confirmation = this.Confirmation |> Option.map _.ToDomain()
          Date = DateOnly.FromDateTime(this.DateTime)
          Time = TimeOnly.FromDateTime(this.DateTime)
          Description = this.Description }

type ConfirmationOption() =

    member val Type: string = String.Empty with get, set
    member val DateStart: Nullable<DateTime> = Nullable() with get, set
    member val DateEnd: Nullable<DateTime> = Nullable() with get, set

    static member private FIRST_AVAILABLE = nameof FirstAvailable
    static member private LAST_AVAILABLE = nameof LastAvailable
    static member private DATE_TIME_RANGE = nameof DateTimeRange

    member this.ToDomain() =
        match this.Type with
        | ConfirmationOption.FIRST_AVAILABLE -> FirstAvailable |> Ok
        | ConfirmationOption.LAST_AVAILABLE -> LastAvailable |> Ok
        | ConfirmationOption.DATE_TIME_RANGE ->
            match this.DateStart |> Option.ofNullable, this.DateEnd |> Option.ofNullable with
            | Some min, Some max -> DateTimeRange(min, max) |> Ok
            | _ -> $"{nameof this.DateStart} or {nameof this.DateEnd}" |> NotFound |> Error
        | _ -> $"The %s{this.Type} of {nameof ConfirmationOption} " |> NotSupported |> Error

type ConfirmationState() =

    member val Type: string = String.Empty with get, set
    member val ConfirmationOption: ConfirmationOption option = None with get, set
    member val AppointmentId: Guid option = None with get, set

    static member private DISABLED = nameof Disabled
    static member private MANUAL = nameof Manual
    static member private AUTO = nameof Auto

    member this.ToDomain() =
        match this.Type with
        | ConfirmationState.DISABLED -> Disabled |> Ok
        | ConfirmationState.MANUAL ->
            match this.AppointmentId with
            | Some id -> id |> AppointmentId |> Manual |> Ok
            | None -> nameof AppointmentId |> NotFound |> Error
        | ConfirmationState.AUTO ->
            match this.ConfirmationOption with
            | Some option -> option.ToDomain() |> Result.map Auto
            | None -> nameof ConfirmationOption |> NotFound |> Error
        | _ -> $"The %s{this.Type} of {nameof ConfirmationState}" |> NotSupported |> Error

type ProcessState() =

    member val Type: string = String.Empty with get, set
    member val Error: External.Error option = None with get, set
    member val Message: string option = None with get, set

    static member private CREATED = nameof Created
    static member private IN_PROCESS = nameof InProcess
    static member private COMPLETED = nameof Completed
    static member private FAILED = nameof Failed

    member this.ToDomain() =
        match this.Type with
        | ProcessState.CREATED -> Created |> Ok
        | ProcessState.IN_PROCESS -> InProcess |> Ok
        | ProcessState.COMPLETED ->
            let msg =
                match this.Message with
                | Some(AP.IsString value) -> value
                | _ -> "Message not found."

            Completed msg |> Ok
        | ProcessState.FAILED ->
            match this.Error with
            | Some error -> error |> Mapper.Error.toInternal |> Result.map Failed
            | None -> "Failed state without error" |> NotSupported |> Error
        | _ -> $"The %s{this.Type} of {nameof ProcessState}" |> NotSupported |> Error

type Service() =
    member val Name: string = String.Empty with get, set
    member val Payload: string = String.Empty with get, set
    member val EmbassyId: string = String.Empty with get, set
    member val EmbassyName: string = String.Empty with get, set
    member val Description: string option = None with get, set

    member this.ToDomain() =
        { Name = this.Name
          Payload = this.Payload
          Embassy =
            { Id = this.EmbassyId |> Graph.NodeIdValue
              Name = this.EmbassyName
              Description = None }
          Description = this.Description }

type Request() =
    member val Id: Guid = Guid.Empty with get, set
    member val Service: Service = Service() with get, set
    member val Attempt: int = 0 with get, set
    member val AttemptModified: DateTime = DateTime.UtcNow with get, set
    member val ProcessState: ProcessState = ProcessState() with get, set
    member val ConfirmationState: ConfirmationState = ConfirmationState() with get, set
    member val Appointments: Appointment array = [||] with get, set
    member val Modified: DateTime = DateTime.UtcNow with get, set

    member this.ToDomain() =
        let result = ResultBuilder()

        result {

            let! processState = this.ProcessState.ToDomain()
            let! confirmationState = this.ConfirmationState.ToDomain()

            return
                { Id = this.Id |> RequestId
                  Service = this.Service.ToDomain()
                  Attempt = this.AttemptModified, this.Attempt
                  ProcessState = processState
                  ConfirmationState = confirmationState
                  Appointments = this.Appointments |> Seq.map _.ToDomain() |> Set.ofSeq
                  Modified = this.Modified }
        }

module private InMemory =
    open Persistence.InMemory

    let private loadData = Query.Json.get<Request> Name

    let createOrUpdate (request: EA.Core.Domain.Request) client =
        client
        |> loadData
        |> Result.map (Seq.filter (fun x -> x.Id <> request.Id))
        |> Result.map (Seq.append [request])
        |> async.Return
    
    let findEmbassiesByRequestIds (ids: RequestId seq) client =
        let requestIds = ids |> Seq.map _.Value |> Set.ofSeq

        client
        |> loadData
        |> Result.map (Seq.filter (fun x -> requestIds.Contains x.Id))
        |> Result.map (Seq.map _.Service.ToDomain())
        |> Result.map (Seq.map _.Embassy)
        |> Result.map List.ofSeq
        |> async.Return

module private FileSystem =
    open Persistence.FileSystem

    let private loadData = Query.Json.get<Request>

    let createOrUpdate (request: EA.Core.Domain.Request) client =
        client
        |> loadData
        |> Result.map (Seq.filter (fun x -> x.Id <> request.Id))
        |> Result.map (Seq.append [request])
        |> async.Return
    
    let findEmbassiesByRequestIds (ids: RequestId seq) client =
        let requestIds = ids |> Seq.map _.Value |> Set.ofSeq

        client
        |> loadData
        |> ResultAsync.map (Seq.filter (fun x -> requestIds.Contains x.Id))
        |> ResultAsync.map (Seq.map _.Service.ToDomain())
        |> ResultAsync.map (Seq.map _.Embassy)
        |> ResultAsync.map List.ofSeq

let private toPersistenceStorage storage =
    storage
    |> function
        | RequestStorage storage -> storage

let init storageType =
    match storageType with
    | FileSystem filePath ->
        { Persistence.Domain.FileSystem.FilePath = filePath
          Persistence.Domain.FileSystem.FileName = Name }
        |> Connection.FileSystem
        |> Persistence.Storage.create
    | InMemory -> Connection.InMemory |> Persistence.Storage.create
    |> Result.map RequestStorage

let findEmbassiesByRequestIds requestIds storage =
    match storage |> toPersistenceStorage with
    | Storage.InMemory client -> client |> InMemory.findEmbassiesByRequestIds requestIds
    | Storage.FileSystem client -> client |> FileSystem.findEmbassiesByRequestIds requestIds
    | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
    
let createOrUpdate request storage =
    match storage |> toPersistenceStorage with
    | Storage.InMemory client -> client |> InMemory.createOrUpdate request
    | Storage.FileSystem client -> client |> FileSystem.createOrUpdate request
    | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
