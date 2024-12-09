module EA.Core.DataAccess.ProcessState

open System
open Infrastructure
open Persistence.DataAccess
open EA.Core.Domain

[<Literal>]
let private CREATED = nameof Created

[<Literal>]
let private IN_PROCESS = nameof InProcess

[<Literal>]
let private COMPLETED = nameof Completed

[<Literal>]
let private FAILED = nameof Failed

type internal ProcessStateEntity() =

    member val Type = String.Empty with get, set
    member val Error: ErrorEntity option = None with get, set
    member val Message: string option = None with get, set

    member this.ToDomain() =
        match this.Type with
        | CREATED -> Created |> Ok
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
            | None -> "Failed state without error" |> NotSupported |> Result.Error
        | _ -> $"The %s{this.Type} of {nameof ProcessStateEntity}" |> NotSupported |> Result.Error

type internal ProcessState with
    member internal this.ToEntity() =
        let result = ProcessStateEntity()

        match this with
        | Created -> result.Type <- CREATED
        | InProcess -> result.Type <- IN_PROCESS
        | Completed msg ->
            result.Type <- COMPLETED
            result.Message <- Some msg
        | Failed error ->
            result.Type <- FAILED
            result.Error <- error.toEntity() |> Some

        result
