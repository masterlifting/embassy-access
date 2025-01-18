module EA.Telegram.Endpoints.Consumer.Embassies.Request

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Endpoints.Consumer.Embassies

type Request =
    | Get of Get.Request

    member this.Value =
        match this with
        | Get r -> r.Value

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER

        match parts[0][0] with
        | '0' -> parts |> Get.Request.parse |> Result.map Get
        | _ -> $"'{input}' of Embassies endpoint" |> NotSupported |> Error
