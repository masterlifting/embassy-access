module EA.Telegram.Endpoints.Consumer.Embassies.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain

[<Literal>]
let private Delimiter = "|"

type KdmidPostModel =
    { ServiceId: Graph.NodeId
      EmbassyId: Graph.NodeId
      Confirmation: ConfirmationState option
      Payload: string }

type MidpassPostModel = { Number: string }

type PostRequest =
    | Kdmid of KdmidPostModel
    | Midpass of MidpassPostModel

    member this.Code =
        match this with
        | Kdmid model ->
            match model.Confirmation with
            | None -> [ "10"; model.ServiceId.Value; model.EmbassyId.Value; model.Payload ]
            | Some confirmation ->
                match confirmation with
                | Disabled -> [ "11"; model.ServiceId.Value; model.EmbassyId.Value; model.Payload ]
                | Manual appointmentId ->
                    [ "12"
                      model.ServiceId.Value
                      model.EmbassyId.Value
                      appointmentId.ValueStr
                      model.Payload ]
                | Auto option ->
                    match option with
                    | FirstAvailable -> [ "13"; model.ServiceId.Value; model.EmbassyId.Value; model.Payload ]
                    | LastAvailable -> [ "14"; model.ServiceId.Value; model.EmbassyId.Value; model.Payload ]
                    | DateTimeRange(start, finish) ->
                        [ "15"
                          model.ServiceId.Value
                          model.EmbassyId.Value
                          start |> string
                          finish |> string
                          model.Payload ]
        | Midpass model -> [ "16"; model.Number ]
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
        | [| "10"; serviceId; embassyId; payload |] -> createKdmid serviceId embassyId payload None
        | [| "11"; serviceId; embassyId; payload |] -> createKdmid serviceId embassyId payload (Some Disabled)
        | [| "12"; serviceId; embassyId; appointmentId; payload |] ->
            appointmentId
            |> AppointmentId.create
            |> Result.bind (fun appointmentId -> createKdmid serviceId embassyId payload (Some(Manual appointmentId)))
        | [| "13"; serviceId; embassyId; payload |] ->
            createKdmid serviceId embassyId payload (Some(Auto FirstAvailable))
        | [| "14"; serviceId; embassyId; payload |] ->
            createKdmid serviceId embassyId payload (Some(Auto LastAvailable))
        | [| "15"; serviceId; embassyId; start; finish; payload |] ->
            match start, finish with
            | AP.IsDateTime start, AP.IsDateTime finish ->
                createKdmid serviceId embassyId payload (Some(Auto(DateTimeRange(start, finish))))
            | _ ->
                $"start: {start} or finish: {finish} of RussianEmbassy.PostRequest endpoint"
                |> NotSupported
                |> Error
        | [| "16"; number |] -> { Number = number } |> PostRequest.Midpass |> Ok
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
