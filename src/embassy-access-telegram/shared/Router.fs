namespace EA.Telegram.Router

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain

[<AutoOpen>]
module private Helpers =
    [<Literal>]
    let private DELIMITER = "|"
    let inline join parts = parts |> String.concat DELIMITER
    let inline split (input: string) = input.Split DELIMITER
    let inline remaining parts = parts |> Array.skip 1 |> join
    let inline notSupported endpoint input =
        $"'{input}' of '{endpoint}' endpoint is not supported." |> NotSupported |> Error
    let inline parseRequestId value = value |> UUID16 |> RequestId
    let inline parseServiceId value =
        value |> Tree.NodeId.create |> ServiceId
    let inline parseEmbassyId value =
        value |> Tree.NodeId.create |> EmbassyId
    let inline parseAppointmentId value = value |> UUID16 |> AppointmentId

module Culture =

    type Get =
        | Cultures

        member this.Value =
            match this with
            | Cultures -> [ "0" ] |> join
        static member parse(input: string) =
            match split input with
            | [| "0" |] -> Cultures |> Ok
            | _ -> notSupported "Culture.Get" input

    type Post =
        | SetCulture of Culture
        | SetCultureCallback of string * Culture

        member this.Value =
            match this with
            | SetCulture culture -> [ "0"; culture.Code ]
            | SetCultureCallback(callback, culture) -> [ "1"; callback; culture.Code ]
            |> String.concat "'"
        static member parse(input: string) =
            match input.Split "'" with
            | [| "0"; code |] -> code |> Culture.parse |> SetCulture |> Ok
            | [| "1"; route; code |] -> (route, code |> Culture.parse) |> SetCultureCallback |> Ok
            | _ -> notSupported "Culture.Post" input

    type Route =
        | Get of Get
        | Post of Post

        member this.Value =
            (match this with
             | Get r -> [ "0"; r.Value ]
             | Post r -> [ "1"; r.Value ])
            |> join
        static member parse(input: string) =
            let parts = split input
            match parts[0] with
            | "0" -> remaining parts |> Get.parse |> Result.map Get
            | "1" -> remaining parts |> Post.parse |> Result.map Post
            | _ -> notSupported "Culture" input

module Embassies =

    module Russian =

        module Kdmid =

            type Get =
                | Info of RequestId
                | Menu of RequestId

                member this.Value =
                    match this with
                    | Info id -> [ "0"; id.Value ]
                    | Menu id -> [ "1"; id.Value ]
                    |> join
                static member parse(input: string) =
                    match split input with
                    | [| "0"; id |] -> Info(parseRequestId id) |> Ok
                    | [| "1"; id |] -> Menu(parseRequestId id) |> Ok
                    | _ -> notSupported "Services.Russian.Kdmid.Get" input

            type Post =
                | SetManualRequest of ServiceId * EmbassyId * link: string
                | SetAutoNotifications of ServiceId * EmbassyId * link: string
                | SetAutoBookingFirst of ServiceId * EmbassyId * link: string
                | SetAutoBookingLast of ServiceId * EmbassyId * link: string
                | SetAutoBookingFirstInPeriod of
                    ServiceId *
                    EmbassyId *
                    start: DateTime *
                    finish: DateTime *
                    link: string
                | ConfirmAppointment of RequestId * AppointmentId
                | StartManualRequest of RequestId

                member this.Value =
                    match this with
                    | SetManualRequest(s, e, p) -> [ "0"; s.Value; e.Value; p ]
                    | SetAutoNotifications(s, e, p) -> [ "1"; s.Value; e.Value; p ]
                    | SetAutoBookingFirst(s, e, p) -> [ "2"; s.Value; e.Value; p ]
                    | SetAutoBookingLast(s, e, p) -> [ "3"; s.Value; e.Value; p ]
                    | SetAutoBookingFirstInPeriod(s, e, st, f, p) -> [
                        "4"
                        s.Value
                        e.Value
                        st |> String.fromDateTime
                        f |> String.fromDateTime
                        p
                      ]
                    | ConfirmAppointment(r, a) -> [ "5"; r.Value; a.ValueStr ]
                    | StartManualRequest r -> [ "6"; r.Value ]
                    |> join
                static member parse(input: string) =
                    match split input with
                    | [| "0"; s; e; p |] -> SetManualRequest(parseServiceId s, parseEmbassyId e, p) |> Ok
                    | [| "1"; s; e; p |] -> SetAutoNotifications(parseServiceId s, parseEmbassyId e, p) |> Ok
                    | [| "2"; s; e; p |] -> SetAutoBookingFirst(parseServiceId s, parseEmbassyId e, p) |> Ok
                    | [| "3"; s; e; p |] -> SetAutoBookingLast(parseServiceId s, parseEmbassyId e, p) |> Ok
                    | [| "4"; s; e; st; f; p |] ->
                        match st, f with
                        | AP.IsDateTime st, AP.IsDateTime f ->
                            SetAutoBookingFirstInPeriod(parseServiceId s, parseEmbassyId e, st, f, p) |> Ok
                        | _ -> notSupported "Services.Russian.Kdmid.Post" input
                    | [| "5"; r; a |] -> ConfirmAppointment(parseRequestId r, parseAppointmentId a) |> Ok
                    | [| "6"; r |] -> StartManualRequest(parseRequestId r) |> Ok
                    | _ -> notSupported "Services.Russian.Kdmid.Post" input

            type Delete =
                | Subscription of RequestId

                member this.Value =
                    match this with
                    | Subscription id -> [ "0"; id.Value ] |> join
                static member parse(input: string) =
                    match split input with
                    | [| "0"; id |] -> Subscription(parseRequestId id) |> Ok
                    | _ -> notSupported "Services.Russian.Kdmid.Delete" input

            type Route =
                | Get of Get
                | Post of Post
                | Delete of Delete

                member this.Value =
                    (match this with
                     | Get r -> [ "0"; r.Value ]
                     | Post r -> [ "1"; r.Value ]
                     | Delete r -> [ "2"; r.Value ])
                    |> join
                static member parse(input: string) =
                    let parts = split input
                    match parts[0] with
                    | "0" -> remaining parts |> Get.parse |> Result.map Get
                    | "1" -> remaining parts |> Post.parse |> Result.map Post
                    | "2" -> remaining parts |> Delete.parse |> Result.map Delete
                    | _ -> notSupported "Services.Russian.Kdmid" input

        module Midpass =

            type Get =
                | Info of RequestId
                | Menu of RequestId

                member this.Value =
                    match this with
                    | Info id -> [ "0"; id.Value ]
                    | Menu id -> [ "1"; id.Value ]
                    |> join
                static member parse(input: string) =
                    match split input with
                    | [| "0"; id |] -> Info(parseRequestId id) |> Ok
                    | [| "1"; id |] -> Menu(parseRequestId id) |> Ok
                    | _ -> notSupported "Services.Russian.Midpass.Get" input

            type Post =
                | CheckStatus of ServiceId * EmbassyId * number: string

                member this.Value =
                    match this with
                    | CheckStatus(s, e, n) -> [ "0"; s.Value; e.Value; n ] |> join
                static member parse(input: string) =
                    match split input with
                    | [| "0"; s; e; n |] -> CheckStatus(parseServiceId s, parseEmbassyId e, n) |> Ok
                    | _ -> notSupported "Services.Russian.Midpass.Post" input

            type Delete =
                | Subscription of RequestId

                member this.Value =
                    match this with
                    | Subscription id -> [ "0"; id.Value ] |> join
                static member parse(input: string) =
                    match split input with
                    | [| "0"; id |] -> Subscription(parseRequestId id) |> Ok
                    | _ -> notSupported "Services.Russian.Midpass.Delete" input

            type Route =
                | Get of Get
                | Post of Post
                | Delete of Delete

                member this.Value =
                    (match this with
                     | Get r -> [ "0"; r.Value ]
                     | Post r -> [ "1"; r.Value ]
                     | Delete r -> [ "2"; r.Value ])
                    |> join
                static member parse(input: string) =
                    let parts = split input
                    match parts[0] with
                    | "0" -> remaining parts |> Get.parse |> Result.map Get
                    | "1" -> remaining parts |> Post.parse |> Result.map Post
                    | "2" -> remaining parts |> Delete.parse |> Result.map Delete
                    | _ -> notSupported "Services.Russian.Midpass" input

        type Route =
            | Kdmid of Kdmid.Route
            | Midpass of Midpass.Route

            member this.Value =
                (match this with
                 | Kdmid r -> [ "0"; r.Value ]
                 | Midpass r -> [ "1"; r.Value ])
                |> join
            static member parse(input: string) =
                let parts = split input
                match parts[0] with
                | "0" -> remaining parts |> Kdmid.Route.parse |> Result.map Kdmid
                | "1" -> remaining parts |> Midpass.Route.parse |> Result.map Midpass
                | _ -> notSupported "Services.Russian" input

    module Italian =

        module Prenotami =

            type Get =
                | Info of RequestId
                | Menu of RequestId

                member this.Value =
                    match this with
                    | Info id -> [ "0"; id.Value ]
                    | Menu id -> [ "1"; id.Value ]
                    |> join
                static member parse(input: string) =
                    match split input with
                    | [| "0"; id |] -> Info(parseRequestId id) |> Ok
                    | [| "1"; id |] -> Menu(parseRequestId id) |> Ok
                    | _ -> notSupported "Services.Italian.Prenotami.Get" input

            type Post =
                | SetManualRequest of ServiceId * EmbassyId * login: string * password: string
                | SetAutoNotifications of ServiceId * EmbassyId * login: string * password: string
                | ConfirmAppointment of RequestId * AppointmentId
                | StartManualRequest of RequestId

                member this.Value =
                    match this with
                    | SetManualRequest(s, e, l, p) -> [ "0"; s.Value; e.Value; l; p ]
                    | SetAutoNotifications(s, e, l, p) -> [ "1"; s.Value; e.Value; l; p ]
                    | ConfirmAppointment(r, a) -> [ "2"; r.Value; a.ValueStr ]
                    | StartManualRequest r -> [ "3"; r.Value ]
                    |> join
                static member parse(input: string) =
                    match split input with
                    | [| "0"; s; e; l; p |] -> SetManualRequest(parseServiceId s, parseEmbassyId e, l, p) |> Ok
                    | [| "1"; s; e; l; p |] -> SetAutoNotifications(parseServiceId s, parseEmbassyId e, l, p) |> Ok
                    | [| "2"; r; a |] -> ConfirmAppointment(parseRequestId r, parseAppointmentId a) |> Ok
                    | [| "3"; r |] -> StartManualRequest(parseRequestId r) |> Ok
                    | _ -> notSupported "Services.Italian.Prenotami.Post" input

            type Delete =
                | Subscription of RequestId

                member this.Value =
                    match this with
                    | Subscription id -> [ "0"; id.Value ] |> join
                static member parse(input: string) =
                    match split input with
                    | [| "0"; id |] -> Subscription(parseRequestId id) |> Ok
                    | _ -> notSupported "Services.Italian.Prenotami.Delete" input

            type Route =
                | Get of Get
                | Post of Post
                | Delete of Delete

                member this.Value =
                    (match this with
                     | Get r -> [ "0"; r.Value ]
                     | Post r -> [ "1"; r.Value ]
                     | Delete r -> [ "2"; r.Value ])
                    |> join
                static member parse(input: string) =
                    let parts = split input
                    match parts[0] with
                    | "0" -> remaining parts |> Get.parse |> Result.map Get
                    | "1" -> remaining parts |> Post.parse |> Result.map Post
                    | "2" -> remaining parts |> Delete.parse |> Result.map Delete
                    | _ -> notSupported "Services.Italian.Prenotami" input

        type Route =
            | Prenotami of Prenotami.Route

            member this.Value =
                (match this with
                 | Prenotami r -> [ "0"; r.Value ])
                |> join
            static member parse(input: string) =
                let parts = split input
                match parts[0] with
                | "0" -> remaining parts |> Prenotami.Route.parse |> Result.map Prenotami
                | _ -> notSupported "Services.Italian" input

    type Get =
        | Embassies
        | UserEmbassies
        | Embassy of EmbassyId
        | UserEmbassy of EmbassyId
        | Services of EmbassyId
        | UserServices of EmbassyId
        | Service of EmbassyId * ServiceId
        | UserService of EmbassyId * ServiceId

        member this.Value =
            match this with
            | Embassies -> [ "0" ]
            | UserEmbassies -> [ "1" ]
            | Embassy e -> [ "2"; e.Value ]
            | UserEmbassy e -> [ "3"; e.Value ]
            | Services e -> [ "4"; e.Value ]
            | UserServices e -> [ "5"; e.Value ]
            | Service(e, s) -> [ "6"; e.Value; s.Value ]
            | UserService(e, s) -> [ "7"; e.Value; s.Value ]
            |> join
        static member parse(input: string) =
            match split input with
            | [| "0" |] -> Embassies |> Ok
            | [| "1" |] -> UserEmbassies |> Ok
            | [| "2"; e |] -> parseEmbassyId e |> Embassy |> Ok
            | [| "3"; e |] -> parseEmbassyId e |> UserEmbassy |> Ok
            | [| "4"; e |] -> parseEmbassyId e |> Services |> Ok
            | [| "5"; e |] -> parseEmbassyId e |> UserServices |> Ok
            | [| "6"; e; s |] -> (parseEmbassyId e, parseServiceId s) |> Service |> Ok
            | [| "7"; e; s |] -> (parseEmbassyId e, parseServiceId s) |> UserService |> Ok
            | _ -> notSupported "Services.Get" input

    type Route =
        | Get of Get
        | Russian of Russian.Route
        | Italian of Italian.Route

        member this.Value =
            (match this with
             | Get r -> [ "0"; r.Value ]
             | Russian r -> [ "1"; r.Value ]
             | Italian r -> [ "2"; r.Value ])
            |> join
        static member parse(input: string) =
            let parts = split input
            match parts[0] with
            | "0" -> remaining parts |> Get.parse |> Result.map Get
            | "1" -> remaining parts |> Russian.Route.parse |> Result.map Russian
            | "2" -> remaining parts |> Italian.Route.parse |> Result.map Italian
            | _ -> notSupported "Services" input

type Route =
    | Culture of Culture.Route
    | Embassies of Embassies.Route

    member this.Value =
        (match this with
         | Culture r -> [ "0"; r.Value ]
         | Embassies r -> [ "1"; r.Value ])
        |> join
    static member parse(input: string) =
        let parts = split input
        match parts[0] with
        | "0" -> remaining parts |> Culture.Route.parse |> Result.map Culture
        | "1" -> remaining parts |> Embassies.Route.parse |> Result.map Embassies
        | "/culture" -> Culture.Get Culture.Cultures |> Ok |> Result.map Culture
        | "/start" -> Embassies.Get Embassies.Embassies |> Ok |> Result.map Embassies
        | "/mine" -> Embassies.Get Embassies.UserEmbassies |> Ok |> Result.map Embassies
        | _ -> notSupported "Route" input
