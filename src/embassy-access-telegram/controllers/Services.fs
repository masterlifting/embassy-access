[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Services

open Infrastructure.Prelude
open EA.Telegram.Router.Services
open EA.Telegram.Services.Services
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Services
open EA.Telegram.Dependencies.Services.Russian
open EA.Telegram.Dependencies.Services.Italian

module Russian =
    open EA.Telegram.Router.Services.Russian
    open EA.Telegram.Services.Services.Russian

    let respond method =
        fun (deps: Russian.Dependencies) ->
            match method with
            | Method.Kdmid kdmid ->
                deps
                |> Kdmid.Dependencies.create
                |> fun deps ->
                    match kdmid with
                    | Kdmid.Method.Get get ->
                        match get with
                        | Kdmid.Get.Print requestId -> Kdmid.Query.print requestId
                        | Kdmid.Get.Menu requestId -> Kdmid.Query.menu requestId
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes
                    | Kdmid.Method.Post post ->
                        match post with
                        | Kdmid.Post.CheckSlotsNow(serviceId, embassyId, link) ->
                            Kdmid.Command.checkSlotsNow serviceId embassyId link
                        | Kdmid.Post.SlotsAutoNotification(serviceId, embassyId, link) ->
                            Kdmid.Command.slotsAutoNotification serviceId embassyId link
                        | Kdmid.Post.BookFirstSlot(serviceId, embassyId, link) ->
                            Kdmid.Command.bookFirstSlot serviceId embassyId link
                        | Kdmid.Post.BookLastSlot(serviceId, embassyId, link) ->
                            Kdmid.Command.bookLastSlot serviceId embassyId link
                        | Kdmid.Post.BookFirstSlotInPeriod(serviceId, embassyId, start, finish, link) ->
                            Kdmid.Command.bookFirstSlotInPeriod serviceId embassyId start finish link
                        | Kdmid.Post.ConfirmAppointment(requestId, appointmentId) ->
                            Kdmid.Command.confirmAppointment requestId appointmentId
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes
                    | Kdmid.Method.Delete delete ->
                        match delete with
                        | Kdmid.Delete.Subscription requestId -> Kdmid.Command.delete requestId
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes
            | Method.Midpass midpass ->
                deps
                |> Midpass.Dependencies.create
                |> fun deps ->
                    match midpass with
                    | Midpass.Method.Get get ->
                        match get with
                        | Midpass.Get.Print requestId -> Midpass.Query.print requestId
                        | Midpass.Get.Menu requestId -> Midpass.Query.menu requestId
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes
                    | Midpass.Method.Post post ->
                        match post with
                        | Midpass.Post.CheckStatus(serviceId, embassyId, number) ->
                            Midpass.Command.checkStatus serviceId embassyId number
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes
                    | Midpass.Method.Delete delete ->
                        match delete with
                        | Midpass.Delete.Subscription requestId ->

                            Midpass.Command.delete requestId
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes

module Italian =
    open EA.Telegram.Router.Services.Italian
    open EA.Telegram.Services.Services.Italian

    let respond method =
        fun (deps: Italian.Dependencies) ->
            match method with
            | Method.Prenotami prenotami ->
                deps
                |> Prenotami.Dependencies.create
                |> fun deps ->
                    match prenotami with
                    | Prenotami.Method.Get get ->
                        match get with
                        | Prenotami.Get.Print requestId -> Prenotami.Query.print requestId
                        | Prenotami.Get.Menu requestId -> Prenotami.Query.menu requestId
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes
                    | Prenotami.Method.Post post ->
                        match post with
                        | Prenotami.Post.CheckSlotsNow(serviceId, embassyId, login, password) ->
                            Prenotami.Command.checkSlotsNow serviceId embassyId login password
                        | Prenotami.Post.SlotsAutoNotification(serviceId, embassyId, login, password) ->
                            Prenotami.Command.slotsAutoNotification serviceId embassyId login password
                        | Prenotami.Post.ConfirmAppointment(requestId, appointmentId) ->
                            Prenotami.Command.confirmAppointment requestId appointmentId
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes
                    | Prenotami.Method.Delete delete ->
                        match delete with
                        | Prenotami.Delete.Subscription requestId -> Prenotami.Command.delete requestId
                        |> fun f -> deps |> f |> deps.sendTranslatedMessageRes

let respond request chat =
    fun (deps: Request.Dependencies) ->
        deps
        |> Services.Dependencies.create chat
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Method.Get get ->
                match get with
                | Get.Services embassyId -> Query.getServices embassyId
                | Get.Service(embassyId, serviceId) -> Query.getService embassyId serviceId
                | Get.UserServices embassyId -> Query.getUserServices embassyId
                | Get.UserService(embassyId, serviceId) -> Query.getUserService embassyId serviceId
                |> fun f -> deps |> f |> deps.sendTranslatedMessageRes
            | Method.Russian russian -> deps |> Russian.Dependencies.create |> Russian.respond russian
            | Method.Italian italian -> deps |> Italian.Dependencies.create |> Italian.respond italian)
