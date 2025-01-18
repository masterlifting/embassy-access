module EA.Telegram.Endpoints.Consumer.Embassies.Russian.Request

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Endpoints.Consumer.Embassies.Russian

type Request =
    | Get of Get.Request
    | Post of Post.Request

    member this.Value =
        match this with
        | Get r -> r.Value
        | Post r -> r.Value

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER

        match parts[0][0] with
        | '0' -> parts |> Get.Request.parse |> Result.map Get
        | '1' -> parts |> Post.Request.parse |> Result.map Post
        | _ -> $"'{input}' of RussianEmbassy endpoint" |> NotSupported |> Error
