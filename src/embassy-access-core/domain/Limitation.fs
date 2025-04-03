[<AutoOpen>]
module EA.Core.Domain.Limitation

open System
open Infrastructure.Domain

type Limitation = {
    Limit: uint<attempts>
    Period: TimeSpan
}
