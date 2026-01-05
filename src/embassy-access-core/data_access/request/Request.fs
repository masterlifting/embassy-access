[<RequireQualifiedAccess>]
module EA.Core.DataAccess.Request

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.SerDe
open EA.Core.Domain
open Persistence
open EA.Core.DataAccess.ProcessState
open EA.Core.DataAccess.Limit

let private result = ResultBuilder()

type PayloadConverter<'d, 'e> = {
    toDomain: 'e -> Result<'d, Error'>
    toEntity: 'd -> Result<'e, Error'>
}

[<RequireQualifiedAccess>]
type Storage<'d, 'e> = Provider of Storage.Provider * PayloadConverter<'d, 'e>

type Entity() =
    member val Id = String.Empty with get, set
    member val ServiceId = String.Empty with get, set
    member val ServiceName = Array.empty with get, set
    member val ServiceDescription: string | null = null with get, set
    member val EmbassyId = String.Empty with get, set
    member val EmbassyName = Array.empty with get, set
    member val EmbassyDescription: string | null = null with get, set
    member val Payload = String.Empty with get, set
    member val ProcessState = String.Empty with get, set
    member val Limits = String.Empty with get, set
    member val Created = DateTime.UtcNow with get, set
    member val Modified = DateTime.UtcNow with get, set

    member this.ToDomain(payloadConverter: PayloadConverter<_, 'e>) =
        result {

            let! requestId = RequestId.create this.Id

            let! payload = this.Payload |> Json.deserialize<'e> |> Result.bind payloadConverter.toDomain

            let! processState =
                this.ProcessState
                |> Json.deserialize<ProcessState.Entity>
                |> Result.bind _.ToDomain()

            let! limits =
                this.Limits
                |> Json.deserialize<Limit.Entity[]>
                |> Result.bind (Seq.map _.ToDomain() >> Result.choose)
                |> Result.map Set.ofSeq

            return {
                Id = requestId
                Service =
                    Tree.Node.create (
                        this.ServiceId,
                        {
                            NameParts = this.ServiceName |> Array.toList
                            Description = this.ServiceDescription |> Option.ofObj
                        }
                    )
                Embassy =
                    Tree.Node.create (
                        this.EmbassyId,
                        {
                            NameParts = this.EmbassyName |> Array.toList
                            Description = this.EmbassyDescription |> Option.ofObj
                        }
                    )
                Payload = payload
                ProcessState = processState
                Limits = limits
                Created = this.Created
                Modified = this.Modified
            }
        }

type private Request<'a> with
    member private this.ToEntity(payloadConverter: PayloadConverter<_, _>) =
        result {
            let! payload = this.Payload |> payloadConverter.toEntity |> Result.bind Json.serialize
            let! processState = this.ProcessState.ToEntity() |> Json.serialize
            let! limits = this.Limits |> Seq.map _.ToEntity() |> Json.serialize

            return
                Entity(
                    Id = this.Id.Value,
                    ServiceId = this.Service.Id.Value,
                    ServiceName = (this.Service.Value.NameParts |> Array.ofList),
                    ServiceDescription = (this.Service.Value.Description |> Option.toObj),
                    EmbassyId = this.Embassy.Id.Value,
                    EmbassyName = (this.Embassy.Value.NameParts |> Array.ofList),
                    EmbassyDescription = (this.Embassy.Value.Description |> Option.toObj),
                    Payload = payload,
                    ProcessState = processState,
                    Limits = limits,
                    Created = this.Created,
                    Modified = this.Modified
                )
        }

// Helper function for external modules to access ToEntity method
let toEntity payloadConverter (request: Request<_>) = request.ToEntity payloadConverter

module internal Common =
    let create (request: Request<_>) payloadConverter (data: Entity array) =
        match data |> Array.exists (fun x -> x.Id = request.Id.Value) with
        | true -> $"The '{request.Id}' already exists." |> AlreadyExists |> Error
        | false ->
            request.ToEntity payloadConverter
            |> Result.map (fun request -> data |> Array.append [| request |])

    let update (request: Request<_>) payloadConverter (data: Entity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = request.Id.Value) with
        | Some index ->
            request.ToEntity payloadConverter
            |> Result.map (fun request ->
                data[index] <- request
                data)
        | None -> $"The '{request.Id}' not found." |> NotFound |> Error

    let delete (id: RequestId) (data: Entity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = id.Value) with
        | Some index -> data |> Array.removeAt index |> Ok
        | None -> $"The '{id}' not found." |> NotFound |> Error
