module EA.Telegram.Endpoints.Consumer.Embassies.Russian.Get

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Core.Domain

type Request =
    | KdmidCheckAppointments of RequestId
    | MidpassCheckStatus of string

    member this.Value =
        match this with
        | KdmidCheckAppointments requestId -> [ "00"; requestId.ValueStr ]
        | MidpassCheckStatus number -> [ "01"; number ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(parts: string[]) =
        match parts with
        | [| "00"; requestId |] -> RequestId.create requestId |> Result.map Request.KdmidCheckAppointments
        | [| "01"; number |] -> Request.MidpassCheckStatus number |> Ok
        | _ -> $"'{parts}' of RussianEmbassy.GetRequest endpoint" |> NotSupported |> Error
