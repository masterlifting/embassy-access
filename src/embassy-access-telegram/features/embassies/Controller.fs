[<RequireQualifiedAccess>]
module EA.Telegram.Features.Controller.Embassies

open Infrastructure.Prelude
open EA.Telegram.Features.Dependencies
open EA.Telegram.Router.Embassies

module Russian =
    open EA.Telegram.Features.Embassies.Russian
    open EA.Telegram.Router.Embassies.Russian
    open EA.Telegram.Features.Dependencies.Embassies.Russian

    let respond route =
        fun (deps: Root.Dependencies) ->
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
                        |> fun f -> deps |> f |> deps.sendMessage
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
                        |> fun f -> deps |> f |> deps.sendMessage
                    | Kdmid.Delete delete ->
                        match delete with
                        | Kdmid.Subscription requestId -> Kdmid.Command.deleteRequest requestId
                        |> fun f -> deps |> f |> deps.sendMessage
            | Midpass request ->
                deps
                |> Midpass.Dependencies.create
                |> fun deps ->
                    match request with
                    | Midpass.Get get ->
                        match get with
                        | Midpass.Info requestId -> Midpass.Query.info requestId
                        | Midpass.Menu requestId -> Midpass.Query.menu requestId
                        |> fun f -> deps |> f |> deps.sendMessage
                    | Midpass.Post post ->
                        match post with
                        | Midpass.CheckStatus(serviceId, embassyId, number) ->
                            Midpass.Command.checkStatus serviceId embassyId number
                        |> fun f -> deps |> f |> deps.sendMessage
                    | Midpass.Delete delete ->
                        match delete with
                        | Midpass.Subscription requestId -> Midpass.Command.deleteRequest requestId
                        |> fun f -> deps |> f |> deps.sendMessage

module Italian =
    open EA.Telegram.Features.Embassies.Italian
    open EA.Telegram.Router.Embassies.Italian
    open EA.Telegram.Features.Dependencies.Embassies.Italian

    let respond route =
        fun (deps: Root.Dependencies) ->
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
                        |> fun f -> deps |> f |> deps.sendMessage
                    | Prenotami.Post post ->
                        match post with
                        | Prenotami.SetManualRequest(serviceId, embassyId, login, password) ->
                            Prenotami.Command.setManualRequest serviceId embassyId login password
                        | Prenotami.SetAutoNotifications(serviceId, embassyId, login, password) ->
                            Prenotami.Command.setAutoNotifications serviceId embassyId login password
                        | Prenotami.ConfirmAppointment(requestId, appointmentId) ->
                            Prenotami.Command.confirmAppointment requestId appointmentId
                        | Prenotami.StartManualRequest requestId -> Prenotami.Command.startManualRequest requestId
                        |> fun f -> deps |> f |> deps.sendMessage
                    | Prenotami.Delete delete ->
                        match delete with
                        | Prenotami.Subscription requestId -> Prenotami.Command.deleteRequest requestId
                        |> fun f -> deps |> f |> deps.sendMessage

open EA.Telegram.Dependencies
open EA.Telegram.Features.Embassies

let respond (request: Route) chat =
    fun (deps: Request.Dependencies) ->
        deps
        |> Embassies.Root.Dependencies.create chat
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Embassies -> Query.getEmbassies ()
                | UserEmbassies -> Query.getUserEmbassies ()
                | Embassy embassyId -> Query.getEmbassy embassyId
                | UserEmbassy embassyId -> Query.getUserEmbassy embassyId
                | Services embassyId -> Query.getServices embassyId
                | UserServices embassyId -> Query.getUserServices embassyId
                | Service(embassyId, serviceId) -> Query.getService embassyId serviceId
                | UserService(embassyId, serviceId) -> Query.getUserService embassyId serviceId
                |> fun f -> deps |> f |> deps.sendMessage
            | Russian russian -> deps |> Embassies.Russian.Root.Dependencies.create |> Russian.respond russian
            | Italian italian -> deps |> Embassies.Italian.Root.Dependencies.create |> Italian.respond italian)
