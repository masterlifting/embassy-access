[<AutoOpen>]
module EA.Core.Domain.ProcessState

open Infrastructure.Domain

type ProcessState =
    | Ready
    | InProcess
    | Completed of string
    | Failed of Error'

    static member print(state: ProcessState) =
        match state with
        | Ready -> "Ready to process"
        | InProcess -> "In process"
        | Completed message -> $"Completed. {message}"
        | Failed error -> $"Failed. {error}"
