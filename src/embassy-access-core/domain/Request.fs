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
    Created: DateTime
    Modified: DateTime
} with

    static member inline print(request: Request<_>) =
        let limits =
            request.Limits
            |> Set.toList
            |> function
                | [] -> "No limits"
                | limits ->
                    limits
                    |> Seq.sortBy _.Period
                    |> Seq.sortBy _.Attempts
                    |> Seq.map Limit.print
                    |> String.concat "\n - "
                    |> sprintf "\n - %s"
        let state = request.ProcessState |> ProcessState.print
        let embassyName = request.Embassy.BuildName 1 "->"
        let serviceName = request.Service.BuildName 2 "->"

        $"[Subscription] '%s{request.Id.ValueStr}'"
        + $"\n[Embassy] %s{embassyName}"
        + $"\n[Service] %s{serviceName}"
        + $"\n[Created] '%s{request.Created.AddHours request.Embassy.TimeZone |> String.fromDateTime}'"
        + $"\n[Modified] '%s{request.Modified.AddHours request.Embassy.TimeZone |> String.fromDateTime}'"
        + $"\n[Last state] %s{state}"
        + $"\n[Limits] %s{limits}"

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
