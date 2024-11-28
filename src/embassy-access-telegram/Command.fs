module EA.Telegram.Command

open EA.Core.Domain
open Infrastructure
open Infrastructure.Logging

[<Literal>]
let private Delimiter = "|"

module private Code =
    [<Literal>]
    let START = "/start"

    [<Literal>]
    let MINE = "/mine"

    [<Literal>]
    let GET_EMBASSY = "/001"

    [<Literal>]
    let GET_SERVICE = "/002"

type Name =
    | GetEmbassies
    | GetEmbassy of Graph.NodeId
    | GetService of Graph.NodeId * Graph.NodeId
    | ChooseAppointments of Graph.NodeId * AppointmentId

let private build args = args |> String.concat Delimiter

let private printSize (value: string) =
    let size = System.Text.Encoding.UTF8.GetByteCount(value)
    $"'{value}' -> {size}" |> Log.info
    value

let set command =
    match command with
    | GetEmbassies -> Code.START
    | GetEmbassy embassyId -> [ Code.GET_EMBASSY; embassyId.Value |> string ] |> build
    | GetService(embassyId, serviceId) ->
        [ Code.GET_SERVICE; embassyId.Value |> string; serviceId.Value |> string ]
        |> build
    | _ -> ""

let get (value: string) =
    let parts = value.Split Delimiter

    match parts.Length with
    | 0 -> Ok None
    | _ ->
        let argsLength = parts.Length - 1

        match parts[0] with
        | Code.START -> Ok <| Some GetEmbassies
        | Code.GET_EMBASSY ->
            match argsLength with
            | 1 ->
                let embassyId = parts[1] |> Graph.NodeIdValue
                GetEmbassy embassyId |> Some |> Ok
            | _ -> Ok <| None
        | Code.GET_SERVICE ->
            match argsLength with
            | 2 ->
                let embassyId = parts[1] |> Graph.NodeIdValue
                let serviceId = parts[2] |> Graph.NodeIdValue
                GetService(embassyId, serviceId) |> Some |> Ok
            | _ -> Ok <| None
        | _ -> Ok <| None
