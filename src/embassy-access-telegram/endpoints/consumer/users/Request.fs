module EA.Telegram.Endpoints.Consumer.Users.Request

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Endpoints.Consumer.Users

type Request =
    | Get of Get.Request

    member this.Value =
        match this with
        | Get r -> r.Value

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER

        match parts[0][0] with
        | '0' -> parts |> Get.Request.parse |> Result.map Get
        | _ -> $"'{input}' of Users endpoint" |> NotSupported |> Error
