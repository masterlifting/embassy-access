module EA.Telegram.Router.Services.Russian.Midpass.Post

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Core.Domain

module Models =

    type Subscribe = {
        Number: string
        ServiceId: ServiceId
        EmbassyId: EmbassyId
        UseBackground: bool
    } with

        member this.Serialize() = [
            this.ServiceId.ValueStr
            this.EmbassyId.ValueStr
            match this.UseBackground with
            | true -> "1"
            | false -> "0"
            this.Number
        ]

        static member deserialize(parts: string list) =
            match parts with
            | [ serviceId; embassyId; useBackground; payload ] ->
                match useBackground with
                | "0" -> false |> Ok
                | "1" -> true |> Ok
                | _ ->
                    $"'{useBackground}' of 'Services.Russian.Midpass.Post.Subscribe' endpoint is not supported."
                    |> NotSupported
                    |> Error
                |> Result.map (fun useBackground -> {
                    Number = payload
                    ServiceId = serviceId |> Graph.NodeIdValue |> ServiceId
                    EmbassyId = embassyId |> Graph.NodeIdValue |> EmbassyId
                    UseBackground = useBackground
                })
            | _ ->
                $"'{parts}' of 'Services.Russian.Midpass.Post.Subscribe' endpoint is not supported."
                |> NotSupported
                |> Error

    type CheckStatus = {
        Number: string
        ServiceId: ServiceId
        EmbassyId: EmbassyId
    } with

        member this.Serialize() = [ this.ServiceId.ValueStr; this.EmbassyId.ValueStr; this.Number ]

        static member deserialize(parts: string list) =
            match parts with
            | [ serviceId; embassyId; payload ] ->
                {
                    Number = payload
                    ServiceId = serviceId |> Graph.NodeIdValue |> ServiceId
                    EmbassyId = embassyId |> Graph.NodeIdValue |> EmbassyId
                }
                |> Ok
            | _ ->
                $"'{parts}' of 'Services.Russian.Midpass.Post.CheckStatus' endpoint is not supported."
                |> NotSupported
                |> Error
                
    type SendStatus = {
        Status: string
        ServiceId: ServiceId
        EmbassyId: EmbassyId
    } with

        member this.Serialize() = [ this.ServiceId.ValueStr; this.EmbassyId.ValueStr; this.Status ]

        static member deserialize(parts: string list) =
            match parts with
            | [ serviceId; embassyId; status ] ->
                {
                    Status = status
                    ServiceId = serviceId |> Graph.NodeIdValue |> ServiceId
                    EmbassyId = embassyId |> Graph.NodeIdValue |> EmbassyId
                }
                |> Ok
            | _ ->
                $"'{parts}' of 'Services.Russian.Midpass.Post.SendStatus' endpoint is not supported."
                |> NotSupported
                |> Error

open Models

type Route =
    | Subscribe of Subscribe
    | CheckStatus of CheckStatus
    | SendStatus of SendStatus

    member this.Value =
        match this with
        | Subscribe model -> "0" :: model.Serialize()
        | CheckStatus model -> "1" :: model.Serialize()
        | SendStatus model -> "2" :: model.Serialize()
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; serviceId; embassyId; useBackground; payload |] ->
            [ serviceId; embassyId; useBackground; payload ]
            |> Subscribe.deserialize
            |> Result.map Route.Subscribe
        | [| "1"; serviceId; embassyId; payload |] ->
            [ serviceId; embassyId; payload ]
            |> CheckStatus.deserialize
            |> Result.map Route.CheckStatus
        | [| "2"; serviceId; embassyId; status |] ->
            [ serviceId; embassyId; status ]
            |> SendStatus.deserialize
            |> Result.map Route.SendStatus
        | _ ->
            $"'{parts}' of 'Services.Russian.Midpass.Post' endpoint is not supported."
            |> NotSupported
            |> Error
