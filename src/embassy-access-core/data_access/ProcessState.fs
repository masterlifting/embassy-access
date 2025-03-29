module EA.Core.DataAccess.ProcessState

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.DataAccess
open EA.Core.Domain

[<Literal>]
let private READY = nameof Ready

[<Literal>]
let private IN_PROCESS = nameof InProcess

[<Literal>]
let private COMPLETED = nameof Completed

[<Literal>]
let private FAILED = nameof Failed

type ProcessStateEntity() =

    member val Type = String.Empty with get, set
    member val Error: ErrorEntity option = None with get, set
    member val Message: string option = None with get, set

    member this.ToDomain() =
        match this.Type with
        | READY -> Ready |> Ok
        | IN_PROCESS -> InProcess |> Ok
        | COMPLETED ->
            let msg =
                match this.Message with
                | Some(AP.IsString value) -> value
                | _ -> "Message not found."

            Completed msg |> Ok
        | FAILED ->
            match this.Error with
            | Some error -> error.ToDomain() |> Result.map Failed
            | None -> $"{nameof ProcessStateEntity} failed state without error is not supported." |> NotSupported |> Error
        | _ -> $"The '%s{this.Type}' of '{nameof ProcessStateEntity}' is not supported." |> NotSupported |> Error

type internal ProcessState with
    member internal this.ToEntity() =
        let result = ProcessStateEntity()

        match this with
        | Ready -> result.Type <- READY
        | InProcess -> result.Type <- IN_PROCESS
        | Completed msg ->
            result.Type <- COMPLETED
            result.Message <- Some msg
        | Failed error ->
            result.Type <- FAILED
            result.Error <- error.ToEntity() |> Some

        result
