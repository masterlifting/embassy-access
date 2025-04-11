module EA.Worker.DataAccess.Embassies.Russian

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Worker.Domain.Embassies.Russian
open Persistence
open Persistence.Storages.Domain

type Storage = Storage of Storage.Provider

type StorageType = Configuration of Configuration.Connection

type KdmidSubdomainEntity() =
    member val Name: string = String.Empty with get, set
    member val EmbassyId: string = String.Empty with get, set

    member this.ToDomain() =
        this.EmbassyId
        |> Graph.NodeId.create
        |> Result.map (fun embassyId -> {
            Name = this.Name
            EmbassyId = embassyId
        })

module private Configuration =
    open Persistence.Storages.Configuration

    let getKdmidSubdomains client =
        client
        |> Read.section<KdmidSubdomainEntity array>
        |> Result.bind (Seq.map _.ToDomain() >> Result.choose)
        |> async.Return

let private toPersistenceStorage storage =
    storage
    |> function
        | Storage storage -> storage

let init storageType =
    match storageType with
    | Configuration connection ->
        connection
        |> Storage.Connection.Configuration
        |> Storage.init
        |> Result.map Storage

let getKdmidSubdomains storage =
    match storage |> toPersistenceStorage with
    | Storage.Configuration client -> client |> Configuration.getKdmidSubdomains
    | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return
