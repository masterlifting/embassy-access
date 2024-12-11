module EA.Telegram.Command

open EA.Core.Domain
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging

[<Literal>]
let private Delimiter = "|"

module private Code =
    [<Literal>]
    let START = "/start"

    [<Literal>]
    let MINE = "/mine"

    [<Literal>]
    let GET_EMBASSY = "/EMB-GET"

    [<Literal>]
    let GET_USER_EMBASSY = "/EMB-USER-GET"

    [<Literal>]
    let GET_SERVICE = "/SRV-GET"

    [<Literal>]
    let SET_SERVICE = "/SRV-SET"

type Name =
    | GetEmbassies
    | GetUserEmbassies
    | GetUserEmbassy of Graph.NodeId
    | GetEmbassy of Graph.NodeId
    | GetService of Graph.NodeId * Graph.NodeId
    | SetService of Graph.NodeId * Graph.NodeId * string
    | ChooseAppointments of Graph.NodeId * AppointmentId

let private build args = args |> String.concat Delimiter

let private printSize (value: string) =
    let size = System.Text.Encoding.UTF8.GetByteCount(value)
    $"'{value}' -> {size}" |> Log.info
    value

let set command =
    match command with
    | GetEmbassies -> Code.START
    | GetUserEmbassies -> Code.MINE
    | GetEmbassy embassyId -> [ Code.GET_EMBASSY; embassyId.Value |> string ] |> build
    | GetUserEmbassy embassyId -> [ Code.GET_USER_EMBASSY; embassyId.Value |> string ] |> build
    | GetService(embassyId, serviceId) ->
        [ Code.GET_SERVICE; embassyId.Value |> string; serviceId.Value |> string ]
        |> build
    | SetService(embassyId, serviceId, payload) ->
        [ Code.SET_SERVICE
          embassyId.Value |> string
          serviceId.Value |> string
          payload ]
        |> build
    | _ -> System.String.Empty

let get (value: string) =
    let parts = value.Split Delimiter

    match parts.Length with
    | 0 -> Ok None
    | _ ->
        let argsLength = parts.Length - 1

        match parts[0] with
        | Code.START -> Ok <| Some GetEmbassies
        | Code.MINE -> Ok <| Some GetUserEmbassies
        | Code.GET_EMBASSY ->
            match argsLength with
            | 1 ->
                let embassyId = parts[1] |> Graph.NodeIdValue
                GetEmbassy embassyId |> Some |> Ok
            | _ -> Ok <| None
        | Code.GET_USER_EMBASSY ->
            match argsLength with
            | 1 ->
                let embassyId = parts[1] |> Graph.NodeIdValue
                GetUserEmbassy embassyId |> Some |> Ok
            | _ -> Ok <| None
        | Code.GET_SERVICE ->
            match argsLength with
            | 2 ->
                let embassyId = parts[1] |> Graph.NodeIdValue
                let serviceId = parts[2] |> Graph.NodeIdValue
                GetService(embassyId, serviceId) |> Some |> Ok
            | _ -> Ok <| None
        | Code.SET_SERVICE ->
            match argsLength with
            | 3 ->
                let embassyId = parts[1] |> Graph.NodeIdValue
                let serviceId = parts[2] |> Graph.NodeIdValue

                match parts[3] with
                | AP.IsString payload -> SetService(embassyId, serviceId, payload) |> Some |> Ok
                | _ -> parts[3] |> NotSupported |> Error

            | _ -> Ok <| None
        | _ -> Ok <| None
