module EA.Telegram.Endpoints.Consumer.Embassies.Russian.Kdmid.Request

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Endpoints.Consumer.Embassies.Russian.Kdmid

type Request =
    | Get of Get.Request
    | Post of Post.Request
    | Delete of Delete.Request

    member this.Value =
        match this with
        | Get r -> [ "0"; r.Value ]
        | Post r -> [ "1"; r.Value ]
        | Delete r -> [ "2"; r.Value ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER
        let remaining = parts[1..] |> String.concat Constants.Endpoint.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Get.Request.parse |> Result.map Get
        | "1" -> remaining |> Post.Request.parse |> Result.map Post
        | "2" -> remaining |> Delete.Request.parse |> Result.map Delete
        | _ -> $"'{input}' of Consumer.Embassies.Russian endpoint" |> NotSupported |> Error
