[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Culture

open Infrastructure.Prelude
open EA.Telegram.Endpoints.Culture
open EA.Telegram.Endpoints.Culture.Request
open EA.Telegram.Dependencies
open EA.Telegram.Services.Culture

let respond request entrypoint =
    fun (deps: Request.Dependencies) ->
        match request with
        | Get get ->
            match get with
            | Get.Cultures -> deps |> Query.getCultures () |> deps.sendMessageRes
        | Post post ->
            match post with
            | Post.SetCulture culture -> deps |> Command.setCulture culture |> deps.sendMessageRes
            | Post.SetCultureCallback(callback, culture) ->
                deps
                |> Command.setCultureCallback culture
                |> ResultAsync.bindAsync (fun _ ->
                    EA.Telegram.Endpoints.Request.Request.parse callback
                    |> ResultAsync.wrap (fun route -> deps |> entrypoint route))

let apply (request: EA.Telegram.Endpoints.Request.Request) callback =
    fun (deps: Request.Dependencies) ->
        deps.tryGetChat ()
        |> ResultAsync.bindAsync (function
            | Some chat -> deps |> callback chat
            | None -> deps |> Query.getCulturesCallback request.Value |> deps.sendMessageRes)
