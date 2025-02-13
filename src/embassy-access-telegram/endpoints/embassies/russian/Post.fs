module EA.Telegram.Endpoints.Embassies.Russian.Post

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Endpoints.Embassies.Russian

type Request =
    | Kdmid of Kdmid.Post.Request

    member this.Value =
        match this with
        | Kdmid r -> [ "0"; r.Value ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER
        let remaining = parts[1..] |> String.concat Constants.Endpoint.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Kdmid.Post.Request.parse |> Result.map Kdmid
        | _ ->
            $"'{parts}' of Consumer.Embassies.Russian.Post endpoint"
            |> NotSupported
            |> Error
