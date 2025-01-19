[<AutoOpen>]
module EA.Core.Domain.ProcessState

open Infrastructure.Domain

type ProcessState =
    | Ready
    | InProcess
    | Completed of string
    | Failed of Error'
    
    member this.Message =
        match this with
        | Ready -> "Ready"
        | InProcess -> "InProcess"
        | Completed value -> $"Completed: {value}"
        | Failed error -> $"Failed: {error.Message}"
