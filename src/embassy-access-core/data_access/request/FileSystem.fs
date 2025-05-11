module EA.Core.DataAccess.FileSystem.Request

open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open Persistence.Storages.FileSystem

let private loadData = Query.Json.get<Request.Entity<_>>

module Query =

    let getIdentifiers client =
        client
        |> loadData
        |> ResultAsync.bind (Seq.map (fun e -> e.Id |> RequestId.parse) >> Result.choose)

    let tryFindById (id: RequestId) payloadConverter client =
        client
        |> loadData
        |> ResultAsync.map (Seq.tryFind (fun e -> e.Id = id.ValueStr))
        |> ResultAsync.bind (Option.toResult (fun e -> e.ToDomain payloadConverter))

    let findManyByEmbassyId (embassyId: EmbassyId) payloadConverter client =
        client
        |> loadData
        |> ResultAsync.map (Seq.filter (fun e -> e.EmbassyId = embassyId.ValueStr))
        |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)

    let findManyByServiceId (serviceId: ServiceId) payloadConverter client =
        client
        |> loadData
        |> ResultAsync.map (Seq.filter (fun e -> e.ServiceId = serviceId.ValueStr))
        |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)

    let findManyWithServiceId (serviceId: ServiceId) payloadConverter client =
        client
        |> loadData
        |> ResultAsync.map (Seq.filter (fun e -> e.ServiceId.Contains serviceId.ValueStr))
        |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)

    let findManyByIds (ids: RequestId seq) payloadConverter client =
        let idSet = ids |> Seq.map _.ValueStr |> Set.ofSeq

        client
        |> loadData
        |> ResultAsync.map (Seq.filter (fun e -> idSet.Contains e.Id))
        |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)
        |> ResultAsync.map List.ofSeq

    let findManyByEmbassyIdAndServiceId (embassyId: EmbassyId) (serviceId: ServiceId) payloadConverter client =
        client
        |> loadData
        |> ResultAsync.map (Seq.filter (fun e -> e.EmbassyId = embassyId.ValueStr && e.ServiceId = serviceId.ValueStr))
        |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)

module Command =

    let create request payloadConverter client =
        client
        |> loadData
        |> ResultAsync.bind (Request.Common.create request payloadConverter)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
        |> ResultAsync.map (fun _ -> request)

    let update request payloadConverter client =
        client
        |> loadData
        |> ResultAsync.bind (Request.Common.update request payloadConverter)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
        |> ResultAsync.map (fun _ -> request)

    let updateSeq requests payloadConverter client =
        client
        |> loadData
        |> ResultAsync.bind (Request.Common.updateSeq requests payloadConverter)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
        |> ResultAsync.map (fun _ -> requests |> Seq.toList)

    let createOrUpdate request payloadConverter client =
        client
        |> loadData
        |> ResultAsync.bind (fun data ->
            match data |> Seq.exists (fun x -> x.Id = request.Id.ValueStr) with
            | true -> data |> Request.Common.update request payloadConverter
            | false -> data |> Request.Common.create request payloadConverter)
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
