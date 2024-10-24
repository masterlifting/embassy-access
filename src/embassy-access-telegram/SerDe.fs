module EA.Telegram.SerDe

open System.Text

module Emassy =
    open EA.Mapper.Embassy

    let toCompactString (embassy: EA.Domain.Embassy) =
        let external = toExternal embassy
        let sb = StringBuilder()
        sb.Append external.Name |> ignore
        sb.Append '|' |> ignore
        sb.Append external.Country.Name |> ignore
        sb.Append '|' |> ignore
        sb.Append external.Country.City.Name |> ignore
        sb.ToString()

    let fromCompactString (value: string) =
        let parts = value.Split '|'
        let embassy = parts[0]
        let country = parts[1]
        let city = parts[2]
        (embassy, country, city) |> createInternal |> Result.defaultValue (Unchecked.defaultof<_>)
        
