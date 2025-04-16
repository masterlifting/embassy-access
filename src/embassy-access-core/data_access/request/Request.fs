[<RequireQualifiedAccess>]
module EA.Core.DataAccess.Request

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Persistence
open EA.Core.DataAccess.ProcessState
open EA.Core.DataAccess.Limit

type Table<'payload> =
    | Storage of
        {|
            Provider: Storage.Provider
            serializePayload: 'payload -> Result<string, Error'>
            deserializePayload: string -> Result<'payload, Error'>
        |}

type internal Entity() =
    member val Id = String.Empty with get, set
    member val ServiceId = String.Empty with get, set
    member val ServiceName = String.Empty with get, set
    member val ServiceInstruction: string option = None with get, set
    member val ServiceDescription: string option = None with get, set
    member val EmbassyId = String.Empty with get, set
    member val EmbassyName = String.Empty with get, set
    member val EmbassyDescription: string option = None with get, set
    member val EmbassyTimeZone: float = 0. with get, set
    member val Payload = String.Empty with get, set
    member val ProcessState = ProcessStateEntity() with get, set
    member val UseBackground = false with get, set
    member val Limits = Array.empty<LimitEntity> with get, set
    member val Modified = DateTime.UtcNow with get, set

    member this.ToDomain deserializePayload =
        let result = ResultBuilder()

        result {

            let! serviceId = this.ServiceId |> Graph.NodeId.create
            let! embassyId = this.EmbassyId |> Graph.NodeId.create
            let! requestId = RequestId.parse this.Id
            let! processState = this.ProcessState.ToDomain()
            let! limitations = this.Limits |> Seq.map _.ToDomain() |> Result.choose
            let! payload = this.Payload |> deserializePayload

            return {
                Id = requestId
                Service = {
                    Id = serviceId |> ServiceId
                    Name = this.ServiceName
                    Instruction = this.ServiceInstruction
                    Description = this.ServiceDescription
                }
                Embassy = {
                    Id = embassyId |> EmbassyId
                    Name = this.EmbassyName
                    Description = this.EmbassyDescription
                    TimeZone = this.EmbassyTimeZone
                }
                Payload = payload
                ProcessState = processState
                UseBackground = this.UseBackground
                Limits = limitations |> Set.ofSeq
                Modified = this.Modified
            }
        }

type internal Request<'a> with
    member private this.ToEntity serializePayload =
        this.Payload
        |> serializePayload
        |> Result.map (fun payload ->
            Entity(
                Id = this.Id.ValueStr,
                ServiceId = this.Service.Id.ValueStr,
                ServiceName = this.Service.Name,
                ServiceInstruction = this.Service.Instruction,
                ServiceDescription = this.Service.Description,
                EmbassyId = this.Embassy.Id.ValueStr,
                EmbassyName = this.Embassy.Name,
                EmbassyDescription = this.Embassy.Description,
                EmbassyTimeZone = this.Embassy.TimeZone,
                Payload = payload,
                ProcessState = this.ProcessState.ToEntity(),
                UseBackground = this.UseBackground,
                Limits = (this.Limits |> Seq.map _.ToEntity() |> Seq.toArray),
                Modified = this.Modified
            ))

module internal Common =
    let create<'a> (request: Request<'a>) serializePayload (data: Entity array) =
        match data |> Array.exists (fun x -> x.Id = request.Id.ValueStr) with
        | true -> $"The '{request.Id}'" |> AlreadyExists |> Error
        | false ->
            request.ToEntity serializePayload
            |> Result.map (fun request -> data |> Array.append [| request |])

    let update<'a> (request: Request<'a>) serializePayload (data: Entity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = request.Id.ValueStr) with
        | Some index ->
            request.ToEntity serializePayload
            |> Result.map (fun request ->
                data[index] <- request
                data)
        | None -> $"The '{request.Id}' not found." |> NotFound |> Error

    let updateSeq<'a> (requests: Request<'a> seq) serializePayload (data: Entity array) =
        requests
        |> Seq.map (fun request -> data |> update request serializePayload)
        |> Result.choose
        |> Result.map Array.concat

    let delete (id: RequestId) (data: Entity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = id.ValueStr) with
        | Some index -> data |> Array.removeAt index |> Ok
        | None -> $"The '{id}' not found." |> NotFound |> Error
