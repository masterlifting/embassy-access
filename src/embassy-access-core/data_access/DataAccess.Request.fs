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

type private EA.Core.Domain.Confirmation with
    member private this.toEntity() =
        let result = Confirmation()
        result.Description <- this.Description
        result

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

type private EA.Core.Domain.Appointment with
    member private this.toEntity() =
        let result = Appointment()
        result.Id <- this.Id.Value
        result.Value <- this.Value
        result.Confirmation <- this.Confirmation |> Option.map _.toEntity()
        result.DateTime <- this.Date.ToDateTime(this.Time)
        result.Description <- this.Description
        result

type ConfirmationOption() =

    member val Type: string = String.Empty with get, set
    member val DateStart: Nullable<DateTime> = Nullable() with get, set
    member val DateEnd: Nullable<DateTime> = Nullable() with get, set

    static member internal FIRST_AVAILABLE = nameof FirstAvailable
    static member internal LAST_AVAILABLE = nameof LastAvailable
    static member internal DATE_TIME_RANGE = nameof DateTimeRange

    member this.ToDomain() =
        match this.Type with
        | ConfirmationOption.FIRST_AVAILABLE -> FirstAvailable |> Ok
        | ConfirmationOption.LAST_AVAILABLE -> LastAvailable |> Ok
        | ConfirmationOption.DATE_TIME_RANGE ->
            match this.DateStart |> Option.ofNullable, this.DateEnd |> Option.ofNullable with
            | Some min, Some max -> DateTimeRange(min, max) |> Ok
            | _ -> $"{nameof this.DateStart} or {nameof this.DateEnd}" |> NotFound |> Error
        | _ -> $"The %s{this.Type} of {nameof ConfirmationOption} " |> NotSupported |> Error

type private EA.Core.Domain.ConfirmationOption with
    member private this.toEntity() =
        let result = ConfirmationOption()

        match this with
        | FirstAvailable -> result.Type <- ConfirmationOption.FIRST_AVAILABLE
        | LastAvailable -> result.Type <- ConfirmationOption.LAST_AVAILABLE
        | DateTimeRange(min, max) ->
            result.Type <- ConfirmationOption.DATE_TIME_RANGE
            result.DateStart <- Nullable min
            result.DateEnd <- Nullable max

        result

type ConfirmationState() =

    member val Type: string = String.Empty with get, set
    member val ConfirmationOption: ConfirmationOption option = None with get, set
    member val AppointmentId: Guid option = None with get, set

    static member internal DISABLED = nameof Disabled
    static member internal MANUAL = nameof Manual
    static member internal AUTO = nameof Auto

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

type private EA.Core.Domain.ConfirmationState with
    member private this.toEntity() =
        let result = ConfirmationState()
        match this with
        | Disabled -> result.Type <- ConfirmationState.DISABLED
        | Manual appointmentId ->
            result.Type <- ConfirmationState.MANUAL
            result.AppointmentId <- Some appointmentId.Value
        | Auto option ->
            result.Type <- ConfirmationState.AUTO
            result.ConfirmationOption <- Some option |> Option.map _.toEntity()
        result

type ProcessState() =

    member val Type: string = String.Empty with get, set
    member val Error: External.Error option = None with get, set
    member val Message: string option = None with get, set

    static member internal CREATED = nameof Created
    static member internal IN_PROCESS = nameof InProcess
    static member internal COMPLETED = nameof Completed
    static member internal FAILED = nameof Failed

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

type private EA.Core.Domain.ProcessState with
    member private this.toEntity() =
        let result = ProcessState()

        match this with
        | Created -> result.Type <- ProcessState.CREATED
        | InProcess -> result.Type <- ProcessState.IN_PROCESS
        | Completed msg ->
            result.Type <- ProcessState.COMPLETED
            result.Message <- Some msg
        | Failed error ->
            result.Type <- ProcessState.FAILED
            result.Error <- error |> Mapper.Error.toExternal |> Some

        result

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

type private EA.Core.Domain.Service with
    member private this.toEntity() =
        let result = Service()
        result.Name <- this.Name
        result.Payload <- this.Payload
        result.EmbassyId <- this.Embassy.Id.Value
        result.EmbassyName <- this.Embassy.Name
        result.Description <- this.Description
        result

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

type private EA.Core.Domain.Request with
    member private this.toEntity() =
        let result = Request()
        result.Id <- this.Id.Value
        result.Service <- this.Service.toEntity ()
        result.Attempt <- this.Attempt |> snd
        result.AttemptModified <- this.Attempt |> fst
        result.ProcessState <- this.ProcessState.toEntity ()
        result.ConfirmationState <- this.ConfirmationState.toEntity ()
        result.Appointments <- this.Appointments |> Seq.map _.toEntity() |> Seq.toArray
        result.Modified <- this.Modified
        result

module private Common =
    let create (request: EA.Core.Domain.Request) (data: Request array) =
        match data |> Array.exists (fun x -> x.Id = request.Id.Value) with
        | true ->
            Error
            <| Operation
                { Message = $"{request.Id} already exists."
                  Code = Some ErrorCode.ALREADY_EXISTS }
        | false -> data |> Array.append [| request.toEntity () |] |> Ok

    let update (request: EA.Core.Domain.Request) (data: Request array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = request.Id.Value) with
        | Some index ->
            data[index] <- request.toEntity ()
            Ok data
        | None ->
            Error
            <| Operation
                { Message = $"{request.Id} not found."
                  Code = Some ErrorCode.NOT_FOUND }

module private InMemory =
    open Persistence.InMemory

    let private loadData = Query.Json.get<Request> Name

    let create request client =
        client
        |> loadData
        |> Result.bind (Common.create request)
        |> Result.bind (fun data -> client |> Command.Json.save Name data)
        |> async.Return

    let update request client =
        client
        |> loadData
        |> Result.bind (Common.update request)
        |> Result.bind (fun data -> client |> Command.Json.save Name data)
        |> async.Return

    let createOrUpdate request client =
        client
        |> loadData
        |> Result.bind (fun data ->
            match data |> Seq.exists (fun x -> x.Id = request.Id.Value) with
            | true -> data |> Common.update request
            | false -> data |> Common.create request)
        |> Result.bind (fun data -> client |> Command.Json.save Name data)
        |> async.Return

    module Embassy =
        let findManyByRequestIds (ids: RequestId seq) client =
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

    let create request client =
        client
        |> loadData
        |> ResultAsync.bind (Common.create request)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

    let update request client =
        client
        |> loadData
        |> ResultAsync.bind (Common.update request)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

    let createOrUpdate request client =
        client
        |> loadData
        |> ResultAsync.bind (fun data ->
            match data |> Seq.exists (fun x -> x.Id = request.Id.Value) with
            | true -> data |> Common.update request
            | false -> data |> Common.create request)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

    module Embassy =
        let findManyByRequestIds (ids: RequestId seq) client =
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

module Command =
    let create request storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.create request
        | Storage.FileSystem client -> client |> FileSystem.create request
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

    let update request storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.update request
        | Storage.FileSystem client -> client |> FileSystem.update request
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

    let createOrUpdate request storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.createOrUpdate request
        | Storage.FileSystem client -> client |> FileSystem.createOrUpdate request
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

module Query =
    module Embassy =
        let findManyByRequestIds requestIds storage =
            match storage |> toPersistenceStorage with
            | Storage.InMemory client -> client |> InMemory.Embassy.findManyByRequestIds requestIds
            | Storage.FileSystem client -> client |> FileSystem.Embassy.findManyByRequestIds requestIds
            | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
