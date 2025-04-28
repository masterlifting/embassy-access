module EA.Core.DataAccess.InMemory.Request

open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open Persistence.Storages.InMemory

let private loadData = Query.Json.get<Request.Entity<_>>

module Query =

    let getIdentifiers client =
        client
        |> loadData
        |> Result.bind (Seq.map (fun e -> e.Id |> RequestId.parse) >> Result.choose)
        |> async.Return

    let tryFindById (id: RequestId) payloadConverter client =
        client
        |> loadData
        |> Result.map (Seq.tryFind (fun e -> e.Id = id.ValueStr))
        |> Result.bind (Option.toResult (fun e -> e.ToDomain payloadConverter))
        |> async.Return

    let findManyByEmbassyId (embassyId: EmbassyId) payloadConverter client =
        client
        |> loadData
        |> Result.map (Seq.filter (fun e -> e.EmbassyId = embassyId.ValueStr))
        |> Result.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)
        |> async.Return

    let findManyByServiceId (serviceId: ServiceId) payloadConverter client =
        client
        |> loadData
        |> Result.map (Seq.filter (fun e -> e.ServiceId = serviceId.ValueStr))
        |> Result.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)
        |> async.Return

    let findManyWithServiceId (serviceId: ServiceId) payloadConverter client =
        client
        |> loadData
        |> Result.map (Seq.filter (fun e -> e.ServiceId.Contains serviceId.ValueStr))
        |> Result.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)
        |> async.Return

    let findManyByIds (ids: RequestId seq) payloadConverter client =
        let idSet = ids |> Seq.map _.ValueStr |> Set.ofSeq

        client
        |> loadData
        |> Result.map (Seq.filter (fun e -> idSet.Contains e.Id))
        |> Result.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)
        |> Result.map List.ofSeq
        |> async.Return

    let findManyByEmbassyIdAndServiceId (embassyId: EmbassyId) (serviceId: ServiceId) payloadConverter client =
        client
        |> loadData
        |> Result.map (Seq.filter (fun e -> e.EmbassyId = embassyId.ValueStr && e.ServiceId = serviceId.ValueStr))
        |> Result.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)
        |> async.Return

module Command =
    let create request payloadConverter client =
        client
        |> loadData
        |> Result.bind (Request.Common.create request payloadConverter)
        |> Result.bind (fun data -> client |> Command.Json.save data)
        |> Result.map (fun _ -> request)
        |> async.Return

    let update request payloadConverter client =
        client
        |> loadData
        |> Result.bind (Request.Common.update request payloadConverter)
        |> Result.bind (fun data -> client |> Command.Json.save data)
        |> Result.map (fun _ -> request)
        |> async.Return

    let updateSeq requests payloadConverter client =
        client
        |> loadData
        |> Result.bind (Request.Common.updateSeq requests payloadConverter)
        |> Result.bind (fun data -> client |> Command.Json.save data)
        |> Result.map (fun _ -> requests |> Seq.toList)
        |> async.Return

    let createOrUpdate request payloadConverter client =
        client
        |> loadData
        |> Result.bind (fun data ->
            match data |> Seq.exists (fun e -> e.Id = request.Id.ValueStr) with
            | true -> data |> Request.Common.update request payloadConverter
            | false -> data |> Request.Common.create request payloadConverter)
        |> Result.bind (fun data -> client |> Command.Json.save data)
        |> Result.map (fun _ -> request)
        |> async.Return

    let delete (id: RequestId) client =
        client
        |> loadData
        |> Result.bind (Request.Common.delete id)
        |> Result.bind (fun data -> client |> Command.Json.save data)
        |> async.Return

    let deleteMany (ids: RequestId seq) client =
        let idSet = ids |> Seq.map _.ValueStr |> Set.ofSeq

        client
        |> loadData
        |> Result.map (Array.filter (fun e -> not (idSet.Contains e.Id)))
        |> Result.bind (fun data -> client |> Command.Json.save data)
        |> async.Return
