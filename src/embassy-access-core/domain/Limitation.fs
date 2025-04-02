[<AutoOpen>]
module EA.Core.Domain.Limitation

open System
open Infrastructure.Domain

type Limitation = {
    Count: uint<attempts>
    Period: TimeSpan
}
