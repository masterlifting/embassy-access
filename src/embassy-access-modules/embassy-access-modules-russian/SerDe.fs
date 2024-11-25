module EA.Embassies.Russian.SerDe

open Infrastructure

module ServiceName =
    [<Literal>]
    let private DELIMITER = ","

    let serialize serviceName =
        serviceName
        |> Graph.splitNodeName
        |> Seq.length
        |> fun length -> string |> Seq.init length
        |> String.concat DELIMITER


    let deserialize (value: string) = value |> int
