[<RequireQualifiedAccess>]
module EA.Telegram.Features.Controller.Culture

open Infrastructure.Prelude
open EA.Telegram.Dependencies
open EA.Telegram.Features.Router.Culture
open EA.Telegram.Features.Services.Culture

let respond request entrypoint =
    fun (deps: Request.Dependencies) ->
        match request with
        | Get get ->
            match get with
            | Cultures -> deps |> Query.getCultures () |> deps.sendMessage
        | Post post ->
            match post with
            | SetCulture culture -> deps |> Command.setCulture culture |> deps.sendMessageRes
            | SetCultureCallback(callback, culture) ->
                deps
                |> Command.setCultureCallback culture
                |> ResultAsync.bindAsync (fun _ ->
                    Route.parse callback |> ResultAsync.wrap (fun route -> deps |> entrypoint route))

let apply (request: Route) callback =
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
