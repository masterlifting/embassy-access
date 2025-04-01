module EA.Russian.Client

open EA.Russian.Clients
open EA.Russian.Clients.Domain

type Provider =
    | Kdmid of Kdmid.Client
    | Midpass of Midpass.Client

type Dependencies =
    | Kdmid of Kdmid.Dependencies
    | Midpass of Midpass.Dependencies

let init deps =
    match deps with
    | Dependencies.Kdmid value -> value |> Kdmid.Client.init |> Result.map Provider.Kdmid
    | Dependencies.Midpass value -> value |> Midpass.Client.init |> Result.map Provider.Midpass
