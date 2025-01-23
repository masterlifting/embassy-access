module EA.Telegram.Endpoints.Consumer.Embassies.Russian.Kdmid.Delete

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Core.Domain

type Request =
    | Subscription of RequestId

    member this.Value =
        match this with
        | Subscription requestId -> [ "0"; requestId.ValueStr ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER
        
        match parts with
        | [| "0"; requestId |] -> RequestId.create requestId |> Result.map Subscription
        | _ ->
            $"'{parts}' of Consumer.Embassies.Russian.Kdmid.Delete endpoint"
            |> NotSupported
            |> Error