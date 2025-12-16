[<RequireQualifiedAccess>]
module EA.Telegram.Features.Controller.Services

open Infrastructure.Prelude
open EA.Telegram.Features.Dependencies
open EA.Telegram.Features.Router.Services

module Russian =
    open EA.Telegram.Features.Services.Russian
    open EA.Telegram.Features.Router.Services.Russian
    open EA.Telegram.Features.Dependencies.Services.Russian

    let respond route =
        fun (deps: Dependencies) ->
            match route with
            | Kdmid request ->
                deps
                |> Kdmid.Dependencies.create
                |> fun deps ->
                    match request with
                    | Kdmid.Get get ->
                        match get with
                        | Kdmid.Info requestId -> Kdmid.Query.info requestId
                        | Kdmid.Menu requestId -> Kdmid.Query.menu requestId
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes
                    | Kdmid.Post post ->
                        match post with
                        | Kdmid.SetManualRequest(serviceId, embassyId, link) ->
                            Kdmid.Command.setManualRequest serviceId embassyId link
                        | Kdmid.SetAutoNotifications(serviceId, embassyId, link) ->
                            Kdmid.Command.setAutoNotifications serviceId embassyId link
                        | Kdmid.SetAutoBookingFirst(serviceId, embassyId, link) ->
                            Kdmid.Command.setAutoBookingFirst serviceId embassyId link
                        | Kdmid.SetAutoBookingLast(serviceId, embassyId, link) ->
                            Kdmid.Command.setAutoBookingLast serviceId embassyId link
                        | Kdmid.SetAutoBookingFirstInPeriod(serviceId, embassyId, start, finish, link) ->
                            Kdmid.Command.setAutoBookingFirstInPeriod serviceId embassyId start finish link
                        | Kdmid.ConfirmAppointment(requestId, appointmentId) ->
                            Kdmid.Command.confirmAppointment requestId appointmentId
                        | Kdmid.StartManualRequest requestId -> Kdmid.Command.startManualRequest requestId
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes
                    | Kdmid.Delete delete ->
                        match delete with
                        | Kdmid.Subscription requestId -> Kdmid.Command.deleteRequest requestId
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes
            | Midpass request ->
                deps
                |> Midpass.Dependencies.create
                |> fun deps ->
                    match request with
                    | Midpass.Get get ->
                        match get with
                        | Midpass.Info requestId -> Midpass.Query.info requestId
                        | Midpass.Menu requestId -> Midpass.Query.menu requestId
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes
                    | Midpass.Post post ->
                        match post with
                        | Midpass.CheckStatus(serviceId, embassyId, number) ->
                            Midpass.Command.checkStatus serviceId embassyId number
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes
                    | Midpass.Delete delete ->
                        match delete with
                        | Midpass.Subscription requestId -> Midpass.Command.deleteRequest requestId
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes

module Italian =
    open EA.Telegram.Features.Services.Italian
    open EA.Telegram.Features.Router.Services.Italian
    open EA.Telegram.Features.Dependencies.Services.Italian

    let respond route =
        fun (deps: Dependencies) ->
            match route with
            | Prenotami prenotami ->
                deps
                |> Prenotami.Dependencies.create
                |> fun deps ->
                    match prenotami with
                    | Prenotami.Get get ->
                        match get with
                        | Prenotami.Info requestId -> Prenotami.Query.print requestId
                        | Prenotami.Menu requestId -> Prenotami.Query.menu requestId
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes
                    | Prenotami.Post post ->
                        match post with
                        | Prenotami.SetManualRequest(serviceId, embassyId, login, password) ->
                            Prenotami.Command.setManualRequest serviceId embassyId login password
                        | Prenotami.SetAutoNotifications(serviceId, embassyId, login, password) ->
                            Prenotami.Command.setAutoNotifications serviceId embassyId login password
                        | Prenotami.ConfirmAppointment(requestId, appointmentId) ->
                            Prenotami.Command.confirmAppointment requestId appointmentId
                        | Prenotami.StartManualRequest requestId -> Prenotami.Command.startManualRequest requestId
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes
                    | Prenotami.Delete delete ->
                        match delete with
                        | Prenotami.Subscription requestId -> Prenotami.Command.deleteRequest requestId
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes

let respond (request: Route) chat =
    fun (deps: EA.Telegram.Dependencies.Request.Dependencies) ->
        deps
        |> Services.Dependencies.create chat
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Services embassyId -> EA.Telegram.Features.Services.Query.getServices embassyId
                | Service(embassyId, serviceId) -> EA.Telegram.Features.Services.Query.getService embassyId serviceId
                | UserServices embassyId -> EA.Telegram.Features.Services.Query.getUserServices embassyId
                | UserService(embassyId, serviceId) ->
                    EA.Telegram.Features.Services.Query.getUserService embassyId serviceId
                |> fun f -> deps |> f |> deps.sendTranslatedMessageRes
            | Russian russian -> deps |> Services.Russian.Dependencies.create |> Russian.respond russian
            | Italian italian -> deps |> Services.Italian.Dependencies.create |> Italian.respond italian)
