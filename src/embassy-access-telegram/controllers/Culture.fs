[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Culture

open Infrastructure.Prelude
open EA.Telegram.Router
open EA.Telegram.Router.Culture
open EA.Telegram.Dependencies
open EA.Telegram.Services.Culture

let respond request entrypoint =
    fun (deps: Request.Dependencies) ->
        Culture.Dependencies.create deps
        |> fun cultureDeps ->
            match request with
            | Method.Get get ->
                match get with
                | Get.Cultures -> cultureDeps |> Query.getCultures () |> deps.sendMessage
            | Method.Post post ->
                match post with
                | Post.SetCulture culture -> cultureDeps |> Command.setCulture culture |> deps.sendMessageRes
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
                Culture.Dependencies.create deps
                |> fun cultureDeps ->
                    cultureDeps
                    |> Query.getCulturesCallback request.Value
                    |> deps.sendMessage
                    |> ResultAsync.mapErrorAsync (fun error ->
                        cultureDeps
                        |> Query.getCultures ()
                        |> deps.sendMessage
                        |> Async.map (fun _ -> error)))
