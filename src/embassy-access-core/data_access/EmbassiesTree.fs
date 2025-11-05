[<RequireQualifiedAccess>]
module EA.Core.DataAccess.EmbassiesTree

open System
open Infrastructure.Domain
open Infrastructure.Prelude.Tree.Builder
open Persistence
open Persistence.Storages
open Persistence.Storages.Domain
open EA.Core.Domain

type Storage = Provider of Storage.Provider
type StorageType = Configuration of Configuration.Connection

type EmbassiesTreeEntity() =
    member val Id = String.Empty with get, set
    member val Name = String.Empty with get, set
    member val Description: string option = None with get, set
    member val TimeZone: float option = None with get, set
    member val Children: EmbassiesTreeEntity[] | null = [||] with get, set

    member this.ToDomain() =
        let rec toNode names (e: EmbassiesTreeEntity) =
            let node =
                Tree.Node.create (
                    e.Id,
                    {
                        NameParts = names
                        Description = e.Description
                        TimeZone = e.TimeZone |> Option.defaultValue 0.
                    }
                )

            match e.Children with
            | null
            | [||] -> node
            | children ->
                let nodeChildren = children |> Array.map (fun c -> toNode (names @ [ c.Name ]) c)

                node |> withChildren nodeChildren

        this |> toNode [ this.Name ]

module private Configuration =
    open Persistence.Storages.Configuration

    let private loadData = Read.section<EmbassiesTreeEntity>

    let get client =
        client |> loadData |> Result.map _.ToDomain() |> async.Return

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
