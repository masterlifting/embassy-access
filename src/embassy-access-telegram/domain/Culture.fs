[<AutoOpen>]
module EA.Telegram.Domain.Culture

type Culture =
    | English
    | Russian

    static member create value =
        match value with
        | "RU" ->Russian
        | _ -> English
        
    member this.Value =
        match this with
        | English -> "EN"
        | Russian -> "RU"

    static member createDefault() = English
