[<RequireQualifiedAccess>]
module EA.Core.DataAccess.Request

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Persistence
open EA.Core.DataAccess.RequestService
open EA.Core.DataAccess.ProcessState
open EA.Core.DataAccess.SubscriptionState
open EA.Core.DataAccess.ConfirmationState
open EA.Core.DataAccess.Appointment

[<Literal>]
let private Name = "Requests"

type RequestStorage = RequestStorage of Storage.Type

type StorageType =
    | InMemory
    | FileSystem of filepath: string

type RequestEntity() =
    member val Id = Guid.Empty with get, set
    member val Service = RequestServiceEntity() with get, set
    member val Attempt = 0 with get, set
    member val AttemptModified = DateTime.UtcNow with get, set
    member val ProcessState = ProcessStateEntity() with get, set
    member val SubscriptionState = SubscriptionStateEntity() with get, set
    member val ConfirmationState = ConfirmationStateEntity() with get, set
    member val Appointments = Array.empty<AppointmentEntity> with get, set
    member val Modified = DateTime.UtcNow with get, set

    member this.ToDomain() =
        let result = ResultBuilder()

        result {

            let! processState = this.ProcessState.ToDomain()
            let! subscriptionState = this.SubscriptionState.ToDomain()
            let! confirmationState = this.ConfirmationState.ToDomain()

            return
                { Id = this.Id |> RequestId
                  Service = this.Service.ToDomain()
                  Attempt = this.AttemptModified, this.Attempt
                  ProcessState = processState
                  SubscriptionState = subscriptionState
                  ConfirmationState = confirmationState
                  Appointments = this.Appointments |> Seq.map _.ToDomain() |> Set.ofSeq
                  Modified = this.Modified }
        }

type private Request with
    member private this.ToEntity() =
        let result = RequestEntity()
        result.Id <- this.Id.Value
        result.Service <- this.Service.ToEntity()
        result.Attempt <- this.Attempt |> snd
        result.AttemptModified <- this.Attempt |> fst
        result.ProcessState <- this.ProcessState.ToEntity()
        result.SubscriptionState <- this.SubscriptionState.ToEntity()
        result.ConfirmationState <- this.ConfirmationState.ToEntity()
        result.Appointments <- this.Appointments |> Seq.map _.ToEntity() |> Seq.toArray
        result.Modified <- this.Modified
        result

module private Common =
    let create (request: Request) (data: RequestEntity array) =
        match data |> Array.exists (fun x -> x.Id = request.Id.Value) with
        | true -> $"{request.Id}" |> AlreadyExists |> Error
        | false -> data |> Array.append [| request.ToEntity() |] |> Ok

    let update (request: Request) (data: RequestEntity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = request.Id.Value) with
        | Some index ->
            data[index] <- request.ToEntity()
            Ok data
        | None -> $"{request.Id}" |> NotFound |> Error

module private InMemory =
    open Persistence.InMemory

    let private loadData = Query.Json.get<RequestEntity> Name

    module Query =
        let tryFindById (id: RequestId) client =
            client
            |> loadData
            |> Result.map (Seq.tryFind (fun x -> x.Id = id.Value))
            |> Result.bind (Option.toResult _.ToDomain())
            |> async.Return

        let findManyByEmbassyId (embassyId: Graph.NodeId) client =
            client
            |> loadData
            |> Result.map (Seq.filter (fun x -> x.Service.EmbassyId = embassyId.Value))
            |> Result.bind (Seq.map _.ToDomain() >> Result.choose)
            |> async.Return

        let findManyByEmbassyName name client =
            client
            |> loadData
            |> Result.map (Seq.filter (fun x -> x.Service.EmbassyName = name))
            |> Result.bind (Seq.map _.ToDomain() >> Result.choose)
            |> async.Return

        let findManyByIds (ids: RequestId seq) client =
            let requestIds = ids |> Seq.map _.Value |> Set.ofSeq

            client
            |> loadData
            |> Result.map (Seq.filter (fun x -> requestIds.Contains x.Id))
            |> Result.bind (Seq.map _.ToDomain() >> Result.choose)
            |> Result.map List.ofSeq
            |> async.Return

    module Command =
        let create request client =
            client
            |> loadData
            |> Result.bind (Common.create request)
            |> Result.bind (fun data -> client |> Command.Json.save Name data)
            |> Result.map (fun _ -> request)
            |> async.Return

        let update request client =
            client
            |> loadData
            |> Result.bind (Common.update request)
            |> Result.bind (fun data -> client |> Command.Json.save Name data)
            |> Result.map (fun _ -> request)
            |> async.Return

        let createOrUpdate request client =
            client
            |> loadData
            |> Result.bind (fun data ->
                match data |> Seq.exists (fun x -> x.Id = request.Id.Value) with
                | true -> data |> Common.update request
                | false -> data |> Common.create request)
            |> Result.bind (fun data -> client |> Command.Json.save Name data)
            |> Result.map (fun _ -> request)
            |> async.Return

module private FileSystem =
    open Persistence.FileSystem

    let private loadData = Query.Json.get<RequestEntity>

    module Query =
        let tryFindById (id: RequestId) client =
            client
            |> loadData
            |> ResultAsync.map (Seq.tryFind (fun x -> x.Id = id.Value))
            |> ResultAsync.bind (Option.toResult _.ToDomain())

        let findManyByEmbassyId (embassyId: Graph.NodeId) client =
            client
            |> loadData
            |> ResultAsync.map (Seq.filter (fun x -> x.Service.EmbassyId = embassyId.Value))
            |> ResultAsync.bind (Seq.map _.ToDomain() >> Result.choose)

        let findManyByEmbassyName name client =
            client
            |> loadData
            |> ResultAsync.map (Seq.filter (fun x -> x.Service.EmbassyName = name))
            |> ResultAsync.bind (Seq.map _.ToDomain() >> Result.choose)

        let findManyByIds (ids: RequestId seq) client =
            let requestIds = ids |> Seq.map _.Value |> Set.ofSeq

            client
            |> loadData
            |> ResultAsync.map (Seq.filter (fun x -> requestIds.Contains x.Id))
            |> ResultAsync.bind (Seq.map _.ToDomain() >> Result.choose)
            |> ResultAsync.map List.ofSeq

    module Command =

        let create request client =
            client
            |> loadData
            |> ResultAsync.bind (Common.create request)
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
            |> ResultAsync.map (fun _ -> request)

        let update request client =
            client
            |> loadData
            |> ResultAsync.bind (Common.update request)
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
            |> ResultAsync.map (fun _ -> request)

        let createOrUpdate request client =
            client
            |> loadData
            |> ResultAsync.bind (fun data ->
                match data |> Seq.exists (fun x -> x.Id = request.Id.Value) with
                | true -> data |> Common.update request
                | false -> data |> Common.create request)
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
            |> ResultAsync.map (fun _ -> request)

let private toPersistenceStorage storage =
    storage
    |> function
        | RequestStorage storage -> storage

let init storageType =
    match storageType with
    | FileSystem filePath ->
        { Persistence.FileSystem.Domain.FilePath = filePath
          Persistence.FileSystem.Domain.FileName = Name + ".json" }
        |> Storage.Connection.FileSystem
        |> Storage.init
    | InMemory -> Storage.Connection.InMemory |> Storage.init
    |> Result.map RequestStorage

module Command =
    let create request storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.create request
        | Storage.FileSystem client -> client |> FileSystem.Command.create request
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

    let update request storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.update request
        | Storage.FileSystem client -> client |> FileSystem.Command.update request
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

    let createOrUpdate request storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.createOrUpdate request
        | Storage.FileSystem client -> client |> FileSystem.Command.createOrUpdate request
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

module Query =

    let tryFindById id storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.tryFindById id
        | Storage.FileSystem client -> client |> FileSystem.Query.tryFindById id
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

    let findManyByEmbassyId embassyId storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.findManyByEmbassyId embassyId
        | Storage.FileSystem client -> client |> FileSystem.Query.findManyByEmbassyId embassyId
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

    let findManyByEmbassyName name storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.findManyByEmbassyName name
        | Storage.FileSystem client -> client |> FileSystem.Query.findManyByEmbassyName name
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

    let findManyByIds ids storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.findManyByIds ids
        | Storage.FileSystem client -> client |> FileSystem.Query.findManyByIds ids
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
