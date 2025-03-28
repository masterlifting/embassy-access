[<RequireQualifiedAccess>]
module EA.Core.DataAccess.Request

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Persistence
open Persistence.Storages
open Persistence.Storages.Domain
open EA.Core.DataAccess.RequestService
open EA.Core.DataAccess.ProcessState
open EA.Core.DataAccess.SubscriptionState
open EA.Core.DataAccess.ConfirmationState
open EA.Core.DataAccess.Appointment

[<Literal>]
let private Name = "Requests"

type RequestStorage = RequestStorage of Storage.Provider

type StorageType =
    | InMemory
    | FileSystem of FileSystem.Connection

type RequestEntity() =
    member val Id = String.Empty with get, set
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

            let! requestId = RequestId.parse this.Id
            let! processState = this.ProcessState.ToDomain()
            let! subscriptionState = this.SubscriptionState.ToDomain()
            let! confirmationState = this.ConfirmationState.ToDomain()
            let! appointments = this.Appointments |> Seq.map _.ToDomain() |> Result.choose

            return
                { Id = requestId
                  Service = this.Service.ToDomain()
                  Attempt = this.AttemptModified, this.Attempt
                  ProcessState = processState
                  SubscriptionState = subscriptionState
                  ConfirmationState = confirmationState
                  Appointments = appointments |> Set.ofSeq
                  Modified = this.Modified }
        }

type private Request with
    member private this.ToEntity() =
        let result = RequestEntity()
        result.Id <- this.Id.ValueStr
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
        match data |> Array.exists (fun x -> x.Id = request.Id.ValueStr) with
        | true -> $"The '{request.Id}'" |> AlreadyExists |> Error
        | false -> data |> Array.append [| request.ToEntity() |] |> Ok

    let update (request: Request) (data: RequestEntity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = request.Id.ValueStr) with
        | Some index ->
            data[index] <- request.ToEntity()
            Ok data
        | None -> $"The '{request.Id}'" |> NotFound |> Error

    let delete (id: RequestId) (data: RequestEntity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = id.ValueStr) with
        | Some index -> data |> Array.removeAt index |> Ok
        | None -> $"The '{id}'" |> NotFound |> Error

module private InMemory =
    open Persistence.Storages.InMemory

    let private loadData = Query.Json.get<RequestEntity> Name

    module Query =

        let getIdentifiers client =
            client
            |> loadData
            |> Result.bind (Seq.map (fun x -> x.Id |> RequestId.parse) >> Result.choose)
            |> async.Return

        let tryFindById (id: RequestId) client =
            client
            |> loadData
            |> Result.map (Seq.tryFind (fun x -> x.Id = id.ValueStr))
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

        let findManyByServiceId (id: Graph.NodeId) client =
            client
            |> loadData
            |> Result.map (Seq.filter (fun x -> x.Service.ServiceId = id.Value))
            |> Result.bind (Seq.map _.ToDomain() >> Result.choose)
            |> async.Return

        let findManyByIds (ids: RequestId seq) client =
            let requestIds = ids |> Seq.map _.ValueStr |> Set.ofSeq

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
                match data |> Seq.exists (fun x -> x.Id = request.Id.ValueStr) with
                | true -> data |> Common.update request
                | false -> data |> Common.create request)
            |> Result.bind (fun data -> client |> Command.Json.save Name data)
            |> Result.map (fun _ -> request)
            |> async.Return

        let delete (id: RequestId) client =
            client
            |> loadData
            |> Result.bind (Common.delete id)
            |> Result.bind (fun data -> client |> Command.Json.save Name data)
            |> async.Return

        let deleteMany (ids: RequestId Set) client =
            let idSet = ids |> Set.map _.ValueStr

            client
            |> loadData
            |> Result.map (Array.filter (fun request -> not (idSet.Contains(request.Id))))
            |> Result.bind (fun data -> client |> Command.Json.save Name data)
            |> async.Return

module private FileSystem =
    open Persistence.Storages.FileSystem

    let private loadData = Query.Json.get<RequestEntity>

    module Query =

        let getIdentifiers client =
            client
            |> loadData
            |> ResultAsync.bind (Seq.map (fun x -> x.Id |> RequestId.parse) >> Result.choose)

        let tryFindById (id: RequestId) client =
            client
            |> loadData
            |> ResultAsync.map (Seq.tryFind (fun x -> x.Id = id.ValueStr))
            |> ResultAsync.bind (Option.toResult _.ToDomain())

        let findManyByEmbassyId (embassyId: Graph.NodeId) client =
            client
            |> loadData
            |> ResultAsync.map (Seq.filter (fun x -> x.Service.EmbassyId = embassyId.Value))
            |> ResultAsync.bind (Seq.map _.ToDomain() >> Result.choose)

        let findManyByServiceId (id: Graph.NodeId) client =
            client
            |> loadData
            |> ResultAsync.map (Seq.filter (fun x -> x.Service.ServiceId = id.Value))
            |> ResultAsync.bind (Seq.map _.ToDomain() >> Result.choose)

        let findManyByEmbassyName name client =
            client
            |> loadData
            |> ResultAsync.map (Seq.filter (fun x -> x.Service.EmbassyName = name))
            |> ResultAsync.bind (Seq.map _.ToDomain() >> Result.choose)

        let findManyByIds (ids: RequestId seq) client =
            let requestIds = ids |> Seq.map _.ValueStr |> Set.ofSeq

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
                match data |> Seq.exists (fun x -> x.Id = request.Id.ValueStr) with
                | true -> data |> Common.update request
                | false -> data |> Common.create request)
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
            |> ResultAsync.map (fun _ -> request)

        let delete (id: RequestId) client =
            client
            |> loadData
            |> ResultAsync.bind (Common.delete id)
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

        let deleteMany (ids: RequestId Set) client =
            let idSet = ids |> Set.map _.ValueStr

            client
            |> loadData
            |> ResultAsync.map (Array.filter (fun request -> not (idSet.Contains(request.Id))))
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

let private toPersistenceStorage storage =
    storage
    |> function
        | RequestStorage storage -> storage

let init storageType =
    match storageType with
    | FileSystem connection -> connection |> Storage.Connection.FileSystem |> Storage.init
    | InMemory -> Storage.Connection.InMemory |> Storage.init
    |> Result.map RequestStorage


module Query =

    let getIdentifiers storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.getIdentifiers
        | Storage.FileSystem client -> client |> FileSystem.Query.getIdentifiers
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let tryFindById id storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.tryFindById id
        | Storage.FileSystem client -> client |> FileSystem.Query.tryFindById id
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let findManyByEmbassyId embassyId storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.findManyByEmbassyId embassyId
        | Storage.FileSystem client -> client |> FileSystem.Query.findManyByEmbassyId embassyId
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let findManyByEmbassyName name storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.findManyByEmbassyName name
        | Storage.FileSystem client -> client |> FileSystem.Query.findManyByEmbassyName name
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let findManyByServiceId id storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.findManyByServiceId id
        | Storage.FileSystem client -> client |> FileSystem.Query.findManyByServiceId id
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let findManyByIds ids storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.findManyByIds ids
        | Storage.FileSystem client -> client |> FileSystem.Query.findManyByIds ids
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

module Command =

    let create request storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.create request
        | Storage.FileSystem client -> client |> FileSystem.Command.create request
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let update request storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.update request
        | Storage.FileSystem client -> client |> FileSystem.Command.update request
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

    //TODO: Optimize this
    let updateSeq requests storage =
        requests
        |> Seq.map (fun request -> storage |> update request)
        |> Async.Sequential
        |> Async.map Result.choose

    let createOrUpdate request storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.createOrUpdate request
        | Storage.FileSystem client -> client |> FileSystem.Command.createOrUpdate request
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let delete id storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.delete id
        | Storage.FileSystem client -> client |> FileSystem.Command.delete id
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let deleteMany ids storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.deleteMany ids
        | Storage.FileSystem client -> client |> FileSystem.Command.deleteMany ids
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return
