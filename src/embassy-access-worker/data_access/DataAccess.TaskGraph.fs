﻿module internal EA.Worker.DataAccess.TaskGraph

open System
open Infrastructure
open EA.Core.Domain
open Microsoft.Extensions.Configuration
open Persistence.Domain

[<Literal>]
let private Name = "Worker"

type WorkerStorage = WorkerStorage of Storage

type StorageType = Configuration of IConfigurationRoot

// type internal EmbassyEntity() =
//     member val Id = String.Empty with get, set
//     member val Name = String.Empty with get, set
//     member val Description: string option = None with get, set
//     member val Children = Array.empty<EmbassyEntity> with get, set
//
//     member this.ToDomain() =
//         this.Id
//         |> Graph.NodeId.create
//         |> Result.bind (fun nodeId ->
//             match this.Children with
//             | null -> List.empty |> Ok
//             | children -> children |> Seq.map _.ToDomain() |> Result.choose
//             |> Result.map (fun children ->
//                 Graph.Node(
//                     { Id = nodeId
//                       Name = this.Name
//                       Description = this.Description },
//                     children
//                 )))

module private Configuration =
    open Persistence.Configuration

    let private loadData = Query.get<EmbassyEntity>

    let get client =
        client |> loadData Name |> Result.bind _.ToDomain() |> async.Return

let private toPersistenceStorage storage =
    storage
    |> function
        | WorkerStorage storage -> storage

let init storageType =
    match storageType with
    | Configuration configuration ->
        configuration
        |> Connection.Configuration
        |> Persistence.Storage.create
        |> Result.map WorkerStorage

let getGraph storage =
    match storage |> toPersistenceStorage with
    | Storage.Configuration client -> client |> Configuration.get
    | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
