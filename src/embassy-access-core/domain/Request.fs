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
    AutoProcessing: bool
    ProcessState: ProcessState
    Limits: Set<Limit>
    Modified: DateTime
} with

    static member inline print(request: Request<_>) =

        let inline printPayload (payload: 'p) =
            (^p: (static member print: 'p -> string) payload)

        let payload = request.Payload |> printPayload
        let service = request.Service |> Service.print
        let embassy = request.Embassy |> Embassy.print
        let autoProcessing = request.AutoProcessing |> string
        let processState = request.ProcessState |> ProcessState.print
        let limits = request.Limits |> Seq.map Limit.print |> String.concat ", "

        $"RequestId: '%s{request.Id.ValueStr}'"
        + $"{String.addLines 2 + service}"
        + $"{String.addLines 2 + embassy}"
        + $"{String.addLines 2 + payload}"
        + $"{String.addLines 2}Auto processing enabled: '{autoProcessing}'"
        + $"{String.addLines 2 + processState}"
        + $"{String.addLines 2 + limits}"

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
