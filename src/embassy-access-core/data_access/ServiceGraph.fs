module EA.Core.DataAccess.ServiceGraph

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Persistence

type ServiceGraphStorage = ServiceGraphStorage of Storage.Type

type StorageType = Configuration of Configuration.Domain.Client

type ServiceGraphEntity() =
    member val Id: string = String.Empty with get, set
    member val Name: string = String.Empty with get, set
    member val Instruction: string option = None with get, set
    member val Description: string option = None with get, set
    member val Children: ServiceGraphEntity[] | null = [||] with get, set

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
                      ShortName = this.Name |> Graph.Node.Name.split |> Seq.last
                      Instruction = this.Instruction
                      Description = this.Description },
                    children
                )))

module private Configuration =
    open Persistence.Configuration

    let private loadData = Query.get<ServiceGraphEntity>

    let get section client =
        client |> loadData section |> Result.bind _.ToDomain() |> async.Return

let private toPersistenceStorage storage =
    storage
    |> function
        | ServiceGraphStorage storage -> storage

let init storageType =
    match storageType with
    | Configuration connection ->
        connection
        |> Storage.Connection.Configuration
        |> Storage.init
        |> Result.map ServiceGraphStorage

let get storage =
    match storage |> toPersistenceStorage with
    | Storage.Configuration client -> client.Configuration |> Configuration.get client.SectionName
    | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
