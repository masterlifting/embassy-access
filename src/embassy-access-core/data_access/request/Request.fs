[<RequireQualifiedAccess>]
module EA.Core.DataAccess.Request

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Persistence
open EA.Core.DataAccess.ProcessState
open EA.Core.DataAccess.Limit

type PayloadConverter<'d, 'e> = {
    toDomain: 'e -> Result<'d, Error'>
    toEntity: 'd -> Result<'e, Error'>
}

[<RequireQualifiedAccess>]
type Storage<'d, 'e> = Provider of Storage.Provider * PayloadConverter<'d, 'e>

type Entity<'p>() =
    member val Id = String.Empty with get, set
    member val ServiceId = String.Empty with get, set
    member val ServiceName = Array.empty with get, set
    member val ServiceDescription: string option = None with get, set
    member val EmbassyId = String.Empty with get, set
    member val EmbassyName = Array.empty with get, set
    member val EmbassyDescription: string option = None with get, set
    member val EmbassyTimeZone: float = 0. with get, set
    member val Payload: 'p = Unchecked.defaultof<'p> with get, set
    member val ProcessState = ProcessStateEntity() with get, set
    member val Limits = Array.empty<LimitEntity> with get, set
    member val Created = DateTime.UtcNow with get, set
    member val Modified = DateTime.UtcNow with get, set

    member this.ToDomain(payloadConverter: PayloadConverter<_, _>) =
        let result = ResultBuilder()

        result {

            let! requestId = RequestId.create this.Id
            let! processState = this.ProcessState.ToDomain()
            let! limitations = this.Limits |> Seq.map _.ToDomain() |> Result.choose
            let! payload = this.Payload |> payloadConverter.toDomain

            return {
                Id = requestId
                Service =
                    Tree.Node.create (
                        this.ServiceId,
                        {
                            NameParts = this.ServiceName |> Array.toList
                            Description = this.ServiceDescription
                        }
                    )
                Embassy =
                    Tree.Node.create (
                        this.EmbassyId,
                        {
                            NameParts = this.EmbassyName |> Array.toList
                            Description = this.EmbassyDescription
                            TimeZone = this.EmbassyTimeZone
                        }
                    )
                Payload = payload
                ProcessState = processState
                Limits = limitations |> Set.ofSeq
                Created = this.Created
                Modified = this.Modified
            }
        }

type private Request<'a> with
    member private this.ToEntity(payloadConverter: PayloadConverter<_, _>) =
        this.Payload
        |> payloadConverter.toEntity
        |> Result.map (fun payload ->
            Entity(
                Id = this.Id.ValueStr,
                ServiceId = this.Service.Id.Value,
                ServiceName = (this.Service.Value.NameParts |> Array.ofList),
                ServiceDescription = this.Service.Value.Description,
                EmbassyId = this.Embassy.Id.Value,
                EmbassyName = (this.Embassy.Value.NameParts |> Array.ofList),
                EmbassyDescription = this.Embassy.Value.Description,
                EmbassyTimeZone = this.Embassy.Value.TimeZone,
                Payload = payload,
                ProcessState = this.ProcessState.ToEntity(),
                Limits = (this.Limits |> Seq.map _.ToEntity() |> Seq.toArray),
                Created = this.Created,
                Modified = this.Modified
            ))

// Helper function for external modules to access ToEntity method
let toEntity payloadConverter (request: Request<_>) = request.ToEntity payloadConverter

module internal Common =
    let create (request: Request<_>) payloadConverter (data: Entity<_> array) =
        match data |> Array.exists (fun x -> x.Id = request.Id.ValueStr) with
        | true -> $"The '{request.Id}' already exists." |> AlreadyExists |> Error
        | false ->
            request.ToEntity payloadConverter
            |> Result.map (fun request -> data |> Array.append [| request |])

    let update (request: Request<_>) payloadConverter (data: Entity<_> array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = request.Id.ValueStr) with
        | Some index ->
            request.ToEntity payloadConverter
            |> Result.map (fun request ->
                data[index] <- request
                data)
        | None -> $"The '{request.Id}' not found." |> NotFound |> Error

    let delete (id: RequestId) (data: Entity<_> array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = id.ValueStr) with
        | Some index -> data |> Array.removeAt index |> Ok
        | None -> $"The '{id}' not found." |> NotFound |> Error
