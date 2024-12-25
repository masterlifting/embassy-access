module EA.Telegram.Consumer.Endpoints.RussianEmbassy

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
        match parts with
        | [| "11"; number |] -> { Number = number } |> PostRequest.Midpass |> Ok
        | [| "10"; serviceId; embassyId; payload |] ->
            { ServiceId = serviceId |> Graph.NodeIdValue
              EmbassyId = embassyId |> Graph.NodeIdValue
              Payload = payload }
            |> PostRequest.Kdmid
            |> Ok
        | _ -> $"'{parts}' for Services.Russian.PostRequest" |> NotSupported |> Error

type Request =
    | Post of PostRequest

    member this.Route =
        match this with
        | Post r -> r.Code

    static member parse(input: string) =
        let parts = input.Split Delimiter

        match parts[0][0] with
        | '1' -> parts |> PostRequest.parse |> Result.map Post
        | _ -> $"'{input}' route of Services" |> NotSupported |> Error
