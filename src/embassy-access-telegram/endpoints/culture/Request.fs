module EA.Telegram.Endpoints.Culture.Request

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Endpoints.Culture

type Request =
    | Get of Get.Request

    member this.Value =
        match this with
        | Get r -> [ "0"; r.Value ] |> String.concat Constants.Endpoint.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER
        let remaining = parts[1..] |> String.concat Constants.Endpoint.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Get.Request.parse |> Result.map Get
        | _ -> $"'{input}' of Consumer.Culture endpoint" |> NotSupported |> Error