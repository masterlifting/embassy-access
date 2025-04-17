module EA.Telegram.Router.Services.Italian.Prenotami.Post

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Telegram.Domain

module Models =

    type Subscribe = {
        User: string
        Password: string
        ServiceId: ServiceId
        EmbassyId: EmbassyId
        UseBackground: bool
    } with

        member this.Serialize() = [
            this.ServiceId.ValueStr
            this.EmbassyId.ValueStr
            this.User
            this.Password
            match this.UseBackground with
            | true -> "1"
            | false -> "0"
        ]

        static member deserialize(parts: string list) =
            match parts with
            | [ serviceId; embassyId; useBackground; user; password ] ->
                match useBackground with
                | "0" -> false |> Ok
                | "1" -> true |> Ok
                | _ ->
                    $"'{useBackground}' of 'Services.Italian.Prenotami.Post.Subscribe' endpoint is not supported."
                    |> NotSupported
                    |> Error
                |> Result.map (fun useBackground -> {
                    User = user
                    Password = password
                    ServiceId = serviceId |> Graph.NodeIdValue |> ServiceId
                    EmbassyId = embassyId |> Graph.NodeIdValue |> EmbassyId
                    UseBackground = useBackground
                })
            | _ ->
                $"'{parts}' of 'Services.Italian.Prenotami.Post.Subscribe' endpoint is not supported."
                |> NotSupported
                |> Error

    type CheckAppointments = {
        User: string
        Password: string
        ServiceId: ServiceId
        EmbassyId: EmbassyId
    } with

        member this.Serialize() = [ this.ServiceId.ValueStr; this.EmbassyId.ValueStr; this.User; this.Password ]

        static member deserialize(parts: string list) =
            match parts with
            | [ serviceId; embassyId; user; password ] ->
                {
                    User = user
                    Password = password
                    ServiceId = serviceId |> Graph.NodeIdValue |> ServiceId
                    EmbassyId = embassyId |> Graph.NodeIdValue |> EmbassyId
                }
                |> Ok
            | _ ->
                $"'{parts}' of 'Services.Italian.Prenotami.Post.CheckAppointments' endpoint is not supported."
                |> NotSupported
                |> Error
                
    type SendAppointments = {
        ServiceId: ServiceId
        EmbassyId: EmbassyId
        Appointments: Set<AppointmentId>
    } with

        member this.Serialize() = [
            this.ServiceId.ValueStr
            this.EmbassyId.ValueStr
            this.Appointments |> Set.map _.ValueStr |> Set.toArray |> String.concat ","
        ]

        static member deserialize(parts: string list) =
            match parts with
            | [ serviceId; embassyId; appointments ] ->
                appointments.Split ','
                |> Array.map AppointmentId.parse
                |> Array.toList
                |> Result.choose
                |> Result.map (fun appointmentIds -> {
                    ServiceId = serviceId |> Graph.NodeIdValue |> ServiceId
                    EmbassyId = embassyId |> Graph.NodeIdValue |> EmbassyId
                    Appointments = appointmentIds |> Set.ofList
                })
            | _ ->
                $"'{parts}' of 'Services.Italian.Prenotami.Post.SendAppointments' endpoint is not supported."
                |> NotSupported
                |> Error

open Models

type Route =
    | Subscribe of Subscribe
    | CheckAppointments of CheckAppointments
    | SendAppointments of SendAppointments

    member this.Value =
        match this with
        | Subscribe model -> "0" :: model.Serialize()
        | CheckAppointments model -> "1" :: model.Serialize()
        | SendAppointments model -> "2" :: model.Serialize()
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; serviceId; embassyId; useBackground; user; password |] ->
            [ serviceId; embassyId; useBackground; user; password ]
            |> Subscribe.deserialize
            |> Result.map Route.Subscribe
        | [| "1"; serviceId; embassyId; user; password |] ->
            [ serviceId; embassyId; user; password ]
            |> CheckAppointments.deserialize
            |> Result.map Route.CheckAppointments
        | [| "2"; serviceId; embassyId; appointments |] ->
            [ serviceId; embassyId; appointments ]
            |> SendAppointments.deserialize
            |> Result.map Route.SendAppointments
        | _ ->
            $"'{parts}' of 'Services.Italian.Prenotami.Post' endpoint is not supported."
            |> NotSupported
            |> Error
