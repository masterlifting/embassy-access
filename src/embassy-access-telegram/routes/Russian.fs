module EA.Telegram.Routes.Russian

open Infrastructure.Domain

[<Literal>]
let private Delimiter = "|"

type KdmidPostModel =
    { ServiceId: Graph.NodeId
      EmbassyId: Graph.NodeId
      Payload: string }

type MidpassPostModel = { Number: string }

type PostRequest =
    | Kdmid of KdmidPostModel
    | Midpass of MidpassPostModel

    member this.Code =
        match this with
        | Kdmid model -> [ "10"; model.ServiceId.Value; model.EmbassyId.Value; model.Payload ]
        | Midpass model -> [ "11"; model.Number ]
        |> String.concat Delimiter

    static member parse(parts: string[]) =
        match parts.Length with
        | 2 ->
            match parts[0] with
            | "11" -> { Number = parts[1] } |> PostRequest.Midpass |> Ok
            | _ -> $"'{parts}' for Services.Russian.PostRequest" |> NotSupported |> Error
        | 4 ->
            match parts[0] with
            | "10" ->
                { ServiceId = parts[1] |> Graph.NodeIdValue
                  EmbassyId = parts[2] |> Graph.NodeIdValue
                  Payload = parts[3] }
                |> PostRequest.Kdmid
                |> Ok
            | _ -> $"'{parts}' for Services.Russian.PostRequest" |> NotSupported |> Error
        | _ -> $"'{parts}' for Services.Russian.PostRequest" |> NotSupported |> Error

type Request =
    | Post of PostRequest

    member this.Route =
        match this with
        | Post r -> r.Code

    static member parse(input: string) =
        let parts = input.Split Delimiter
        let remaining = parts[1..]

        match parts[0] with
        | "10" -> remaining |> PostRequest.parse |> Result.map Post
        | _ -> $"'{input}' route of Services" |> NotSupported |> Error
