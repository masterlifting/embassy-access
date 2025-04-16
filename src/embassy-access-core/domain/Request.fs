[<AutoOpen>]
module EA.Core.Domain.Request

open System
open Infrastructure.Domain
open Infrastructure.Prelude

type RequestId =
    | RequestId of UUID16

    member this.Value =
        match this with
        | RequestId id -> id

    member this.ValueStr = this.Value.Value

    static member parse value =
        match value with
        | AP.IsUUID16 id -> RequestId id |> Ok
        | _ -> $"RequestId '{value}' is not supported." |> NotSupported |> Error

    static member createNew() = RequestId <| UUID16.createNew ()

type Request<'payload> = {
    Id: RequestId
    Service: Service
    Embassy: Embassy
    Payload: 'payload
    ProcessState: ProcessState
    Limits: Set<Limit>
    UseBackground: bool
    Modified: DateTime
} with

    member this.UpdateLimits() =
        this.Limits
        |> Seq.map Limit.update
        |> fun limits -> {
            this with
                Limits = limits |> Set.ofSeq
        }

    member this.ValidateLimits() =
        this.Limits
        |> Seq.map Limit.validate
        |> Result.choose
        |> Result.map (fun _ -> this)
