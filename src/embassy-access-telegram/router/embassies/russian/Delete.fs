module EA.Telegram.Router.Embassies.Russian.Delete

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Router.Embassies.Russian

type Route =
    | Kdmid of Kdmid.Delete.Route

    member this.Value =
        match this with
        | Kdmid r -> [ "0"; r.Value ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER
        let remaining = parts[1..] |> String.concat Constants.Endpoint.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Kdmid.Delete.Route.parse |> Result.map Kdmid
        | _ -> $"'{parts}' of Embassies.Russian.Delete endpoint" |> NotSupported |> Error
