[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Services

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.Router.Services
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Services
open EA.Telegram.Dependencies.Services.Russian
open EA.Telegram.Dependencies.Services.Italian
open EA.Telegram.Services.Services

let respond request chat =
    fun (deps: Request.Dependencies) ->
        deps
        |> Services.Dependencies.create chat
        |> ResultAsync.wrap (fun deps ->

            match request with
            | Method.Get get ->
                match get with
                | Get.Services embassyId -> deps |> Query.getServices embassyId
                | Get.Service(embassyId, serviceId) -> deps |> Query.getService embassyId serviceId
                | Get.UserServices embassyId -> deps |> Query.getUserServices embassyId
                | Get.UserService(embassyId, serviceId) -> deps |> Query.getUserService embassyId serviceId
                |> deps.sendTranslatedMessageRes
            | Method.Russian russian ->
                match russian with
                | Russian.Method.Kdmid kdmid ->
                    match kdmid with
                    | Russian.Kdmid.Method.Post post ->
                        match post with
                        | Russian.Kdmid.Post.CheckSlotsNow(serviceId, embassyId, link)
                        | Russian.Kdmid.Post.SlotsAutoNotification(serviceId, embassyId, link)
                        | Russian.Kdmid.Post.BookFirstSlot(serviceId, embassyId, link)
                        | Russian.Kdmid.Post.BookLastSlot(serviceId, embassyId, link)
                        | Russian.Kdmid.Post.BookFirstSlotInPeriod(serviceId, embassyId, link) ->
                            "Russian.Kdmid is not implemented." |> NotImplemented |> Error |> async.Return
                | Russian.Method.Midpass midpass ->
                    match midpass with
                    | Russian.Midpass.Method.Post post ->
                        match post with
                        | Russian.Midpass.Post.CheckStatus(serviceId, embassyId, number) ->
                            "Russian.Midpass is not implemented." |> NotImplemented |> Error |> async.Return
            | Method.Italian italian ->
                match italian with
                | Italian.Method.Prenotami prenotami ->
                    match prenotami with
                    | Italian.Prenotami.Method.Post post ->
                        match post with
                        | Italian.Prenotami.Post.CheckSlotsNow(serviceId, embassyId, login, password)
                        | Italian.Prenotami.Post.SlotsAutoNotification(serviceId, embassyId, login, password) ->
                            "Italian.Prenotami is not implemented."
                            |> NotImplemented
                            |> Error
                            |> async.Return)
