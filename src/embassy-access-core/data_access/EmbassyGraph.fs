[<RequireQualifiedAccess>]
module EA.Core.DataAccess.EmbassyGraph

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence
open Persistence.Storages
open Persistence.Storages.Domain
open EA.Core.Domain

type Storage = Provider of Storage.Provider
type StorageType = Configuration of Configuration.Connection

type EmbassyGraphEntity() =
    member val Id = String.Empty with get, set
    member val Name = String.Empty with get, set
    member val Description: string option = None with get, set
    member val TimeZone: float option = None with get, set
    member val Children: EmbassyGraphEntity[] | null = [||] with get, set

    member this.ToDomain() =

        let rec innerLoop names (entity: EmbassyGraphEntity) =
            entity.Id
            |> Graph.NodeId.parse
            |> Result.bind (fun nodeId ->
                match entity.Children with
                | null -> [] |> Ok
                | children ->
                    children
                    |> Seq.map (fun c -> c |> innerLoop (names @ [ c.Name ]))
                    |> Result.choose
                |> Result.map (fun children ->
                    Graph.Node(
                        {
                            Id = nodeId |> EmbassyId
                            NameParts = names
                            Description = entity.Description
                            TimeZone = entity.TimeZone |> Option.defaultValue 0.
                        },
                        children
                    )))

        this |> innerLoop [ this.Name ]

module private Configuration =
    open Persistence.Storages.Configuration

    let private loadData = Read.section<EmbassyGraphEntity>

    let get client =
        client |> loadData |> Result.bind _.ToDomain() |> async.Return

let private toProvider =
    function
    | Provider provider -> provider

let init storageType =
    match storageType with
    | Configuration connection ->
        connection
        |> Storage.Connection.Configuration
        |> Storage.init
        |> Result.map Provider

let get storage =
    let provider = storage |> toProvider
    match provider with
    | Storage.Configuration client -> client |> Configuration.get
    | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return
