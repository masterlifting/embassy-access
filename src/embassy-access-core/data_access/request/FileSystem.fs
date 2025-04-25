module EA.Core.DataAccess.FileSystem.Request

open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open Persistence.Storages.FileSystem

let private loadData = Query.Json.get<Request.Entity>

module Query =

    let getIdentifiers client =
        client
        |> loadData
        |> ResultAsync.bind (Seq.map (fun e -> e.Id |> RequestId.parse) >> Result.choose)

    let tryFindById (id: RequestId) deserializePayload client =
        client
        |> loadData
        |> ResultAsync.map (Seq.tryFind (fun e -> e.Id = id.ValueStr))
        |> ResultAsync.bind (Option.toResult (fun e -> e.ToDomain deserializePayload))

    let findManyByEmbassyId (embassyId: EmbassyId) deserializePayload client =
        client
        |> loadData
        |> ResultAsync.map (Seq.filter (fun e -> e.EmbassyId = embassyId.ValueStr))
        |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain deserializePayload) >> Result.choose)

    let findManyByServiceId (serviceId: ServiceId) deserializePayload client =
        client
        |> loadData
        |> ResultAsync.map (Seq.filter (fun e -> e.ServiceId = serviceId.ValueStr))
        |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain deserializePayload) >> Result.choose)

    let findManyWithServiceId (serviceId: ServiceId) deserializePayload client =
        client
        |> loadData
        |> ResultAsync.map (Seq.filter (fun e -> e.ServiceId.Contains serviceId.ValueStr))
        |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain deserializePayload) >> Result.choose)

    let findManyByIds (ids: RequestId seq) deserializePayload client =
        let idSet = ids |> Seq.map _.ValueStr |> Set.ofSeq

        client
        |> loadData
        |> ResultAsync.map (Seq.filter (fun e -> idSet.Contains e.Id))
        |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain deserializePayload) >> Result.choose)
        |> ResultAsync.map List.ofSeq
        
    let findManyByEmbassyIdAndServiceId (embassyId: EmbassyId) (serviceId: ServiceId) deserializePayload client =
        client
        |> loadData
        |> ResultAsync.map (Seq.filter (fun e -> e.EmbassyId = embassyId.ValueStr && e.ServiceId = serviceId.ValueStr))
        |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain deserializePayload) >> Result.choose)

module Command =

    let create request serializePayload client =
        client
        |> loadData
        |> ResultAsync.bind (Request.Common.create request serializePayload)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
        |> ResultAsync.map (fun _ -> request)

    let update request serializePayload client =
        client
        |> loadData
        |> ResultAsync.bind (Request.Common.update request serializePayload)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
        |> ResultAsync.map (fun _ -> request)

    let updateSeq requests serializePayload client =
        client
        |> loadData
        |> ResultAsync.bind (Request.Common.updateSeq requests serializePayload)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
        |> ResultAsync.map (fun _ -> requests |> Seq.toList)

    let createOrUpdate request serializePayload client =
        client
        |> loadData
        |> ResultAsync.bind (fun data ->
            match data |> Seq.exists (fun x -> x.Id = request.Id.ValueStr) with
            | true -> data |> Request.Common.update request serializePayload
            | false -> data |> Request.Common.create request serializePayload)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
        |> ResultAsync.map (fun _ -> request)

    let delete (id: RequestId) client =
        client
        |> loadData
        |> ResultAsync.bind (Request.Common.delete id)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

    let deleteMany (ids: RequestId Set) client =
        let idSet = ids |> Set.map _.ValueStr

        client
        |> loadData
        |> ResultAsync.map (Array.filter (fun request -> not (idSet.Contains request.Id)))
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
