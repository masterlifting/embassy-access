[<RequireQualifiedAccess>]
module EA.Core.DataAccess.Request

open System
open Infrastructure
open EA.Core.Domain
open Persistence.Domain
open EA.Core.DataAccess.Service
open EA.Core.DataAccess.ProcessState
open EA.Core.DataAccess.ConfirmationState
open EA.Core.DataAccess.Appointment

[<Literal>]
let private Name = "Requests"

type RequestStorage = RequestStorage of Storage

type StorageType =
    | InMemory
    | FileSystem of filepath: string

type internal RequestEntity() =
    member val Id = Guid.Empty with get, set
    member val Service = ServiceEntity() with get, set
    member val Attempt = 0 with get, set
    member val AttemptModified = DateTime.UtcNow with get, set
    member val ProcessState = ProcessStateEntity() with get, set
    member val ConfirmationState = ConfirmationStateEntity() with get, set
    member val Appointments = Array.empty<AppointmentEntity> with get, set
    member val Modified = DateTime.UtcNow with get, set

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

type private Request with
    member private this.ToEntity() =
        let result = RequestEntity()
        result.Id <- this.Id.Value
        result.Service <- this.Service.ToEntity ()
        result.Attempt <- this.Attempt |> snd
        result.AttemptModified <- this.Attempt |> fst
        result.ProcessState <- this.ProcessState.ToEntity ()
        result.ConfirmationState <- this.ConfirmationState.ToEntity ()
        result.Appointments <- this.Appointments |> Seq.map _.ToEntity() |> Seq.toArray
        result.Modified <- this.Modified
        result

module private Common =
    let create (request: Request) (data: RequestEntity array) =
        match data |> Array.exists (fun x -> x.Id = request.Id.Value) with
        | true ->
            Error
            <| Operation
                { Message = $"{request.Id} already exists."
                  Code = Some ErrorCode.ALREADY_EXISTS }
        | false -> data |> Array.append [| request.ToEntity () |] |> Ok

    let update (request: Request) (data: RequestEntity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = request.Id.Value) with
        | Some index ->
            data[index] <- request.ToEntity ()
            Ok data
        | None ->
            Error
            <| Operation
                { Message = $"{request.Id} not found."
                  Code = Some ErrorCode.NOT_FOUND }

module private InMemory =
    open Persistence.InMemory

    let private loadData = Query.Json.get<RequestEntity> Name

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

    let private loadData = Query.Json.get<RequestEntity>

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
