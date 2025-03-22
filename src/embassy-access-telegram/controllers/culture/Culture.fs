[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Culture.Culture

open Infrastructure.Prelude
open EA.Telegram.Endpoints.Culture
open EA.Telegram.Endpoints.Culture.Request
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Services.Consumer.Culture

let respond request entrypoint =
    fun (deps: Request.Dependencies) ->
        match request with
        | Get get ->
            match get with
            | Get.Cultures -> deps.Culture |> Query.getCultures () |> deps.sendResult
        | Post post ->
            match post with
            | Post.SetCulture culture -> deps.Culture |> Command.setCulture culture |> deps.sendResult
            | Post.SetCultureCallback(callback, culture) ->
                deps.Culture
                |> Command.setCultureCallback culture
                |> ResultAsync.bindAsync (fun _ ->
                    EA.Telegram.Endpoints.Request.Request.parse callback
                    |> ResultAsync.wrap (fun route -> deps |> entrypoint route))

let apply (request: EA.Telegram.Endpoints.Request.Request) callback =
    fun (deps: Request.Dependencies) ->
        deps.Culture.tryGetChat ()
        |> ResultAsync.bindAsync (function
            | Some chat -> deps |> callback chat
            | None -> deps.Culture |> Query.getCulturesCallback request.Value |> deps.sendResult)
