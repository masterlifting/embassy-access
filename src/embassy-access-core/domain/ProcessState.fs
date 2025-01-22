[<AutoOpen>]
module EA.Core.Domain.ProcessState

open Infrastructure.Domain

type ProcessState =
    | Ready
    | InProcess
    | Completed of string
    | Failed of Error'