[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Culture

open Infrastructure.Prelude
open EA.Telegram.Router
open EA.Telegram.Router.Culture
open EA.Telegram.Dependencies
open EA.Telegram.Services.Culture

let respond request entrypoint =
    fun (deps: Request.Dependencies) ->
        match request with
        | Method.Get get ->
            match get with
            | Get.Cultures -> deps |> Query.getCultures () |> deps.sendMessageRes
        | Method.Post post ->
            match post with
            | Post.SetCulture culture -> deps |> Command.setCulture culture |> deps.sendMessageRes
            | Post.SetCultureCallback(callback, culture) ->
                deps
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
