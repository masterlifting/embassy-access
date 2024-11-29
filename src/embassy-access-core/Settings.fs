﻿module EA.Core.Settings

open Infrastructure
open EA.Core.Domain

module Embassy =

    [<Literal>]
    let SECTION_NAME = "Embassies"

    let private getConfigData configuration =
        configuration
        |> Configuration.getSection<External.Graph> SECTION_NAME
        |> Option.map Ok
        |> Option.defaultValue (Error <| NotFound $"Section '%s{SECTION_NAME}' in the configuration.")

    let getGraph configuration =
        configuration
        |> getConfigData
        |> Result.bind Mapper.Embassy.toGraph
        |> async.Return