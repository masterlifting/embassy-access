module EA.Core.DataAccess.ServiceGraph

open System
open Microsoft.Extensions.Configuration
open Infrastructure
open EA.Core.Domain
open Persistence.Domain

type ServiceGraphStorage = ServiceGraphStorage of Storage

type StorageType = Configuration of sectionName: string * configuration: IConfigurationRoot

type ServiceGraphEntity() =
    member val Id: string = String.Empty with get, set
    member val Name: string = String.Empty with get, set
    member val Instruction: string option = None with get, set
    member val Description: string option = None with get, set
    member val Children: ServiceGraphEntity[] = [||] with get, set

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
    | Configuration(section, configuration) ->
        (section, configuration)
        |> Connection.Configuration
        |> Persistence.Storage.init
        |> Result.map ServiceGraphStorage

let get storage =
    match storage |> toPersistenceStorage with
    | Storage.Configuration(section, client) -> client |> Configuration.get section
    | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
