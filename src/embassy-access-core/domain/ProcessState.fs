[<AutoOpen>]
module EA.Core.Domain.ProcessState

open Infrastructure.Domain

type ProcessState =
    | Draft
    | Ready
    | InProcess
    | Completed of string
    | Failed of Error'
