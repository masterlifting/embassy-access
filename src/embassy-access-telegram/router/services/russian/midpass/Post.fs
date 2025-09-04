module EA.Telegram.Router.Services.Russian.Midpass.Post

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Domain

type Route =
    | CheckStatus of ServiceId * EmbassyId * number: string

    member this.Value =
        match this with
        | CheckStatus(serviceId, embassyId, number) ->
            [ "0"; serviceId.ValueStr; embassyId.ValueStr; number ]
            |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; serviceId; embassyId; number |] ->
            CheckStatus(
                serviceId |> Tree.NodeIdValue |> ServiceId,
                embassyId |> Tree.NodeIdValue |> EmbassyId,
                number
            )
            |> Ok
        | _ ->
            $"'{input}' of 'Services.Russian.Midpass.Post' endpoint is not supported."
            |> NotSupported
            |> Error
