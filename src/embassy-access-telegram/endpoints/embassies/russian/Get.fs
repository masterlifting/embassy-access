module EA.Telegram.Endpoints.Embassies.Russian.Get

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Endpoints.Embassies.Russian

type Request =
    | Kdmid of Kdmid.Get.Request
    | Midpass of Midpass.Get.Request

    member this.Value =
        match this with
        | Kdmid r -> [ "0"; r.Value ]
        | Midpass r -> [ "1"; r.Value ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER
        let remaining = parts[1..] |> String.concat Constants.Endpoint.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Kdmid.Get.Request.parse |> Result.map Kdmid
        | "1" -> remaining |> Midpass.Get.Request.parse |> Result.map Midpass
        | _ -> $"'{parts}' of Consumer.Embassies.Russian.Get endpoint" |> NotSupported |> Error
