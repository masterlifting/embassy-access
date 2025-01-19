[<RequireQualifiedAccess>]
module EA.Core.DataAccess.EmbassyGraph

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Persistence

[<Literal>]
let private Name = "Embassies"

type EmbassyGraphStorage = EmbassyGraphStorage of Storage.Type

type StorageType = Configuration of Configuration.Domain.Client

type EmbassyGraphEntity() =
    member val Id = String.Empty with get, set
    member val Name = String.Empty with get, set
    member val Description: string option = None with get, set
    member val Children = Array.empty<EmbassyGraphEntity> with get, set

    member this.ToDomain() =
        this.Id
        |> Graph.NodeId.create
        |> Result.bind (fun nodeId ->
            match this.Children with
            | null -> List.empty |> Ok
            | children -> children |> Seq.map _.ToDomain() |> Result.choose
            |> Result.map (fun children ->
                Graph.Node(
                    { Id = nodeId
                      Name = this.Name
                      ShortName = this.Name |> Graph.split |> Seq.last
                      Description = this.Description },
                    children
                )))

module private Configuration =
    open Persistence.Configuration

    let private loadData = Query.get<EmbassyGraphEntity>

    let get section client =
        client |> loadData section |> Result.bind _.ToDomain() |> async.Return

let private toPersistenceStorage storage =
    storage
    |> function
        | EmbassyGraphStorage storage -> storage

let init storageType =
    match storageType with
    | Configuration connection ->
        connection
        |> Storage.Connection.Configuration
        |> Storage.init
        |> Result.map EmbassyGraphStorage

let get storage =
    match storage |> toPersistenceStorage with
    | Storage.Configuration client -> client.Configuration |> Configuration.get client.SectionName
    | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
