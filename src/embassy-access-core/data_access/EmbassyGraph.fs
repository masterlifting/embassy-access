[<RequireQualifiedAccess>]
module EA.Core.DataAccess.EmbassyGraph

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence
open Persistence.Storages
open Persistence.Storages.Domain
open EA.Core.Domain

type EmbassyGraphStorage = EmbassyGraphStorage of Storage.Provider

type StorageType = Configuration of Configuration.Connection

type EmbassyGraphEntity() =
    member val Id = String.Empty with get, set
    member val Name = String.Empty with get, set
    member val Description: string option = None with get, set
    member val TimeZone: float option = None with get, set
    member val Children: EmbassyGraphEntity[] | null = [||] with get, set

    member this.ToDomain() =
        this.Id
        |> Graph.NodeId.create
        |> Result.bind (fun nodeId ->
            match this.Children with
            | null -> List.empty |> Ok
            | children -> children |> Seq.map _.ToDomain() |> Result.choose
            |> Result.map (fun children ->
                Graph.Node(
                    {
                        Id = nodeId
                        Name = this.Name
                        ShortName = this.Name |> Graph.Node.Name.split |> Seq.last
                        Description = this.Description
                        TimeZone = this.TimeZone |> Option.defaultValue 0.
                    },
                    children
                )))

module private Configuration =
    open Persistence.Storages.Configuration

    let private loadData = Read.section<EmbassyGraphEntity>

    let get client =
        client |> loadData |> Result.bind _.ToDomain() |> async.Return

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
    | Storage.Configuration client -> client |> Configuration.get
    | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return
