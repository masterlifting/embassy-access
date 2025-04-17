[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Culture

open Infrastructure.Prelude
open EA.Telegram.Router
open EA.Telegram.Router.Culture
open EA.Telegram.Dependencies
open EA.Telegram.Services.Culture

let respond request entrypoint =
    fun (deps: Request.Dependencies) ->
        deps
        |> Culture.Dependencies.create deps.ct
        |> fun cultureDeps ->
            match request with
            | Method.Get get ->
                match get with
                | Get.Cultures -> cultureDeps |> Query.getCultures deps.ChatId |> deps.sendMessage
            | Method.Post post ->
                match post with
                | Post.SetCulture culture ->
                    cultureDeps
                    |> Command.setCulture culture deps.ChatId deps.MessageId
                    |> deps.sendMessageRes
                | Post.SetCultureCallback(callback, culture) ->
                    cultureDeps
                    |> Command.setCultureCallback culture
                    |> ResultAsync.bindAsync (fun _ ->
                        Router.parse callback
                        |> ResultAsync.wrap (fun route -> deps |> entrypoint route))

let apply (request: Router.Route) callback =
    fun (deps: Request.Dependencies) ->
        deps.tryGetChat ()
        |> ResultAsync.bindAsync (function
            | Some chat -> deps |> callback chat
            | None ->
                deps
                |> Query.getCulturesCallback request.Value
                |> deps.sendMessageRes
                |> ResultAsync.mapErrorAsync (fun error ->
                    deps
                    |> Query.getCultures ()
                    |> deps.sendMessageRes
                    |> Async.map (fun _ -> error)))
