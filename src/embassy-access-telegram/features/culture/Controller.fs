[<RequireQualifiedAccess>]
module EA.Telegram.Features.Culture.Controller

open Infrastructure.Prelude
open EA.Telegram.Features.Culture.Router
open EA.Telegram.Dependencies
open EA.Telegram.Features.Culture

let respond request entrypoint =
    fun (deps: Request.Dependencies) ->
        match request with
        | Method.Get get ->
            match get with
            | Get.Cultures -> deps |> Query.getCultures () |> deps.sendMessage
        | Method.Post post ->
            match post with
            | Post.SetCulture culture -> deps |> Command.setCulture culture |> deps.sendMessageRes
            | Post.SetCultureCallback(callback, culture) ->
                deps
                |> Command.setCultureCallback culture
                |> ResultAsync.bindAsync (fun _ ->
                    EA.Telegram.Router.Router.parse callback
                    |> ResultAsync.wrap (fun route -> deps |> entrypoint route))

let apply (request: EA.Telegram.Router.Router.Route) callback =
    fun (deps: Request.Dependencies) ->
        deps.tryGetChat ()
        |> ResultAsync.bindAsync (function
            | Some chat -> deps |> callback chat
            | None ->
                deps
                |> Query.getCulturesCallback request.Value
                |> deps.sendMessage
                |> ResultAsync.mapErrorAsync (fun error ->
                    deps |> Query.getCultures () |> deps.sendMessage |> Async.map (fun _ -> error)))
