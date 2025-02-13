module EA.Telegram.Endpoints.Embassies.Russian.Midpass.Request

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Endpoints.Embassies.Russian.Midpass

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
        | _ ->
            $"'{input}' of Consumer.Embassies.Russian.Midpass endpoint"
            |> NotSupported
            |> Error
