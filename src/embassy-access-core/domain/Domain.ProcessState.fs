[<AutoOpen>]
module EA.Core.Domain.ProcessState

open Infrastructure

type ProcessState =
    | Created
    | InProcess
    | Completed of string
    | Failed of Error'