module EA.Telegram.Consumer.Endpoints.RussianEmbassy

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain

[<Literal>]
let private Delimiter = "|"

type KdmidPostModel =
    { ServiceId: Graph.NodeId
      EmbassyId: Graph.NodeId
      Confirmation: ConfirmationState
      Payload: string }

type MidpassPostModel = { Number: string }

type PostRequest =
    | Kdmid of KdmidPostModel
    | Midpass of MidpassPostModel

    member this.Code =
        match this with
        | Kdmid model ->
            match model.Confirmation with
            | Disabled -> [ "10"; model.ServiceId.Value; model.EmbassyId.Value; model.Payload ]
            | Manual appointmentId ->
                [ "11"
                  model.ServiceId.Value
                  model.EmbassyId.Value
                  appointmentId.ValueStr
                  model.Payload ]
            | Auto option ->
                match option with
                | FirstAvailable -> [ "12"; model.ServiceId.Value; model.EmbassyId.Value; model.Payload ]
                | LastAvailable -> [ "13"; model.ServiceId.Value; model.EmbassyId.Value; model.Payload ]
                | DateTimeRange(start, finish) ->
                    [ "14"
                      model.ServiceId.Value
                      model.EmbassyId.Value
                      start |> string
                      finish |> string
                      model.Payload ]
        | Midpass model -> [ "15"; model.Number ]
        |> String.concat Delimiter

    static member parse(parts: string[]) =
        let inline createKdmid serviceId embassyId payload confirmation =
            { ServiceId = serviceId |> Graph.NodeIdValue
              EmbassyId = embassyId |> Graph.NodeIdValue
              Payload = payload
              Confirmation = confirmation }
            |> PostRequest.Kdmid
            |> Ok

        match parts with
        | [| "10"; serviceId; embassyId; payload |] -> createKdmid serviceId embassyId payload Disabled
        | [| "11"; serviceId; embassyId; appointmentId; payload |] ->
            appointmentId
            |> AppointmentId.create
            |> Result.bind (fun appointmentId -> createKdmid serviceId embassyId payload (Manual appointmentId))
        | [| "12"; serviceId; embassyId; payload |] -> createKdmid serviceId embassyId payload (Auto FirstAvailable)
        | [| "13"; serviceId; embassyId; payload |] -> createKdmid serviceId embassyId payload (Auto LastAvailable)
        | [| "14"; serviceId; embassyId; start; finish; payload |] ->
            match start, finish with
            | AP.IsDateTime start, AP.IsDateTime finish ->
                createKdmid serviceId embassyId payload (Auto(DateTimeRange(start, finish)))
            | _ -> $"DateTimeRange {start} {finish}" |> NotSupported |> Error
        | [| "15"; number |] -> { Number = number } |> PostRequest.Midpass |> Ok
        | _ -> $"'{parts}' of RussianEmbassy.PostRequest endpoint" |> NotSupported |> Error

type Request =
    | Post of PostRequest

    member this.Route =
        match this with
        | Post r -> r.Code

    static member parse(input: string) =
        let parts = input.Split Delimiter

        match parts[0][0] with
        | '1' -> parts |> PostRequest.parse |> Result.map Post
        | _ -> $"'{input}' of RussianEmbassy endpoint" |> NotSupported |> Error
