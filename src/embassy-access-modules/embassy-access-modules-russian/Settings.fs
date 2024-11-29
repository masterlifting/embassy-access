module EA.Embassies.Russian.Settings

open Infrastructure
open EA.Embassies.Russian.Domain

module ServiceInfo =

    [<Literal>]
    let SECTION_NAME = "RussianEmbassy"

    let private getConfigData configuration =
        configuration
        |> Configuration.getSection<External.ServiceInfo> SECTION_NAME
        |> Option.map Ok
        |> Option.defaultValue (Error <| NotFound $"Section '%s{SECTION_NAME}' in the configuration.")

    let getGraph configuration =
        configuration
        |> getConfigData
        |> Result.bind Mapper.ServiceInfo.toGraph
        |> async.Return
