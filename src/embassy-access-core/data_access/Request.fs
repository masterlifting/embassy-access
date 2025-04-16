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
open EA.Core.DataAccess.ConfirmationState
open EA.Core.DataAccess.Appointment
open EA.Core.DataAccess.Limit

type RequestStorage = RequestStorage of Storage.Provider

type StorageType =
    | InMemory of InMemory.Connection
    | FileSystem of FileSystem.Connection

type RequestEntity() =
    member val Id = String.Empty with get, set
    member val Service = RequestServiceEntity() with get, set
    member val ProcessState = ProcessStateEntity() with get, set
    member val IsBackground = false with get, set
    member val Limits = Array.empty<LimitEntity> with get, set
    member val ConfirmationState = ConfirmationStateEntity() with get, set
    member val Appointments = Array.empty<AppointmentEntity> with get, set
    member val Modified = DateTime.UtcNow with get, set

    member this.ToDomain() =
        let result = ResultBuilder()

        result {

            let! requestId = RequestId.parse this.Id
            let! processState = this.ProcessState.ToDomain()
            let! confirmationState = this.ConfirmationState.ToDomain()
            let! appointments = this.Appointments |> Seq.map _.ToDomain() |> Result.choose
            let! limitations = this.Limits |> Seq.map _.ToDomain() |> Result.choose

            return {
                Id = requestId
                Service = this.Service.ToDomain()
                ProcessState = processState
                UseBackground = this.IsBackground
                Limits = limitations |> Set.ofSeq
                ConfirmationState = confirmationState
                Appointments = appointments |> Set.ofSeq
                Modified = this.Modified
            }
        }

type RequestEntity'() =
    member val Id = String.Empty with get, set
    member val ServiceId = String.Empty with get, set
    member val ServiceName = String.Empty with get, set
    member val ServiceInstruction: string option = None with get, set
    member val ServiceDescription: string option = None with get, set
    member val EmbassyId = String.Empty with get, set
    member val EmbassyName = String.Empty with get, set
    member val EmbassyDescription: string option = None with get, set
    member val EmbassyTimeZone: float = 0. with get, set
    member val Payload = String.Empty with get, set
    member val ProcessState = ProcessStateEntity() with get, set
    member val UseBackground = false with get, set
    member val Limits = Array.empty<LimitEntity> with get, set
    member val Modified = DateTime.UtcNow with get, set

    member this.ToDomain deserializePayload =
        let result = ResultBuilder()

        result {

            let! serviceId = this.ServiceId |> Graph.NodeId.create
            let! embassyId = this.EmbassyId |> Graph.NodeId.create
            let! requestId = RequestId.parse this.Id
            let! processState = this.ProcessState.ToDomain()
            let! limitations = this.Limits |> Seq.map _.ToDomain() |> Result.choose
            let! payload = this.Payload |> deserializePayload

            return {
                Id = requestId
                Service = {
                    Id = serviceId
                    Name = this.ServiceName
                    Instruction = this.ServiceInstruction
                    Description = this.ServiceDescription
                }
                Embassy = {
                    Id = embassyId
                    Name = this.EmbassyName
                    Description = this.EmbassyDescription
                    TimeZone = this.EmbassyTimeZone
                }
                Payload = payload
                ProcessState = processState
                UseBackground = this.UseBackground
                Limits = limitations |> Set.ofSeq
                Modified = this.Modified
            }
        }

type private Request with
    member private this.ToEntity() =
        RequestEntity(
            Id = this.Id.ValueStr,
            Service = this.Service.ToEntity(),
            ProcessState = this.ProcessState.ToEntity(),
            IsBackground = this.UseBackground,
            Limits = (this.Limits |> Seq.map _.ToEntity() |> Seq.toArray),
            ConfirmationState = this.ConfirmationState.ToEntity(),
            Appointments = (this.Appointments |> Seq.map _.ToEntity() |> Seq.toArray),
            Modified = this.Modified
        )
        
type private Request'<'a> with
    member private this.ToEntity serializePayload =
        this.Payload
        |> serializePayload
        |> Result.map (fun payload ->
            RequestEntity'(
                Id = this.Id.ValueStr,
                ServiceId = this.Service.Id.Value,
                ServiceName = this.Service.Name,
                ServiceInstruction = this.Service.Instruction,
                ServiceDescription = this.Service.Description,
                EmbassyId = this.Embassy.Id.Value,
                EmbassyName = this.Embassy.Name,
                EmbassyDescription = this.Embassy.Description,
                EmbassyTimeZone = this.Embassy.TimeZone,
                Payload = payload,
                ProcessState = this.ProcessState.ToEntity(),
                UseBackground = this.UseBackground,
                Limits = (this.Limits |> Seq.map _.ToEntity() |> Seq.toArray),
                Modified = this.Modified
            ))

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
        | None -> $"The '{request.Id}' not found." |> NotFound |> Error

    let updateNew<'a> (request: Request'<'a>) serializePayload (data: RequestEntity' array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = request.Id.ValueStr) with
        | Some index ->
            request.ToEntity serializePayload
            |> Result.map (fun request ->
                data[index] <- request
                data)
        | None -> $"The '{request.Id}' not found." |> NotFound |> Error

    let updateSeq (requests: Request seq) (data: RequestEntity array) =
        requests
        |> Seq.map (fun request -> data |> update request)
        |> Result.choose
        |> Result.map Array.concat

    let delete (id: RequestId) (data: RequestEntity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = id.ValueStr) with
        | Some index -> data |> Array.removeAt index |> Ok
        | None -> $"The '{id}' not found." |> NotFound |> Error

module private InMemory =
    open Persistence.Storages.InMemory

    let private loadData = Query.Json.get<RequestEntity>
    let private loadDataNew = Query.Json.get<RequestEntity'>

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

        let findManyByServiceId (id: Graph.NodeId) client =
            client
            |> loadData
            |> Result.map (Seq.filter (fun x -> x.Service.ServiceId = id.Value))
            |> Result.bind (Seq.map _.ToDomain() >> Result.choose)
            |> async.Return

        let findManyByPartServiceId (id: Graph.NodeId) client =
            client
            |> loadData
            |> Result.map (Seq.filter (fun x -> x.Service.ServiceId.Contains id.Value))
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
            |> Result.bind (fun data -> client |> Command.Json.save data)
            |> Result.map (fun _ -> request)
            |> async.Return

        let update request client =
            client
            |> loadData
            |> Result.bind (Common.update request)
            |> Result.bind (fun data -> client |> Command.Json.save data)
            |> Result.map (fun _ -> request)
            |> async.Return

        let updateNew<'a> (request: Request'<'a>) client =
            client
            |> loadDataNew
            |> Result.bind (Common.updateNew<'a> request)
            |> Result.bind (fun data -> client |> Command.Json.save data)
            |> Result.map (fun _ -> request)
            |> async.Return

        let updateSeq requests client =
            client
            |> loadData
            |> Result.bind (Common.updateSeq requests)
            |> Result.bind (fun data -> client |> Command.Json.save data)
            |> Result.map (fun _ -> requests |> Seq.toList)
            |> async.Return

        let createOrUpdate request client =
            client
            |> loadData
            |> Result.bind (fun data ->
                match data |> Seq.exists (fun x -> x.Id = request.Id.ValueStr) with
                | true -> data |> Common.update request
                | false -> data |> Common.create request)
            |> Result.bind (fun data -> client |> Command.Json.save data)
            |> Result.map (fun _ -> request)
            |> async.Return

        let delete (id: RequestId) client =
            client
            |> loadData
            |> Result.bind (Common.delete id)
            |> Result.bind (fun data -> client |> Command.Json.save data)
            |> async.Return

        let deleteMany (ids: RequestId Set) client =
            let idSet = ids |> Set.map _.ValueStr

            client
            |> loadData
            |> Result.map (Array.filter (fun request -> not (idSet.Contains request.Id)))
            |> Result.bind (fun data -> client |> Command.Json.save data)
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

        let findManyByPartServiceId (id: Graph.NodeId) client =
            client
            |> loadData
            |> ResultAsync.map (Seq.filter (fun x -> x.Service.ServiceId.Contains id.Value))
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

        let updateSeq requests client =
            client
            |> loadData
            |> ResultAsync.bind (Common.updateSeq requests)
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
            |> ResultAsync.map (fun _ -> requests |> Seq.toList)

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
            |> ResultAsync.map (Array.filter (fun request -> not (idSet.Contains request.Id)))
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

let private toPersistenceStorage storage =
    storage
    |> function
        | RequestStorage storage -> storage

let init storageType =
    match storageType with
    | FileSystem connection -> connection |> Storage.Connection.FileSystem |> Storage.init
    | InMemory connection -> connection |> Storage.Connection.InMemory |> Storage.init
    |> Result.map RequestStorage

module Query =

    let getIdentifiers storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.getIdentifiers
        | Storage.FileSystem client -> client |> FileSystem.Query.getIdentifiers
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let tryFindById id storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.tryFindById id
        | Storage.FileSystem client -> client |> FileSystem.Query.tryFindById id
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let findManyByEmbassyId embassyId storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.findManyByEmbassyId embassyId
        | Storage.FileSystem client -> client |> FileSystem.Query.findManyByEmbassyId embassyId
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let findManyByServiceId id storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.findManyByServiceId id
        | Storage.FileSystem client -> client |> FileSystem.Query.findManyByServiceId id
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let findManyByPartServiceId id storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.findManyByPartServiceId id
        | Storage.FileSystem client -> client |> FileSystem.Query.findManyByPartServiceId id
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let findManyByIds ids storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.findManyByIds ids
        | Storage.FileSystem client -> client |> FileSystem.Query.findManyByIds ids
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

module Command =

    let create request storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.create request
        | Storage.FileSystem client -> client |> FileSystem.Command.create request
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let update request storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.update request
        | Storage.FileSystem client -> client |> FileSystem.Command.update request
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let updateNew<'a> (request: Request'<'a>) storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.update request
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let updateSeq requests storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.updateSeq requests
        | Storage.FileSystem client -> client |> FileSystem.Command.updateSeq requests
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let createOrUpdate request storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.createOrUpdate request
        | Storage.FileSystem client -> client |> FileSystem.Command.createOrUpdate request
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let delete id storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.delete id
        | Storage.FileSystem client -> client |> FileSystem.Command.delete id
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let deleteMany ids storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.deleteMany ids
        | Storage.FileSystem client -> client |> FileSystem.Command.deleteMany ids
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return
