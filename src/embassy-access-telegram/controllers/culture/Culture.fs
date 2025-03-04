[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Culture.Culture

open Infrastructure.Prelude
open EA.Telegram.Endpoints.Culture
open EA.Telegram.Endpoints.Culture.Request
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Services.Culture

let respond request entrypoint =
    fun (consumerDeps: Consumer.Dependencies) ->
        Culture.Dependencies.create consumerDeps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Get.Cultures -> deps |> Query.getCultures () |> deps.sendResult
            | Post post ->
                match post with
                | Post.SetCulture culture -> deps |> Command.setCulture culture |> deps.sendResult
                | Post.SetCultureCallback(callback, culture) ->
                    deps
                    |> Command.setCultureCallback culture
                    |> ResultAsync.bindAsync (fun _ ->
                        EA.Telegram.Endpoints.Request.Request.parse callback
                        |> ResultAsync.wrap (fun route -> consumerDeps |> entrypoint route)))

let apply (request: EA.Telegram.Endpoints.Request.Request) callback =
    fun (consumerDeps: Consumer.Dependencies) ->
        Culture.Dependencies.create consumerDeps
        |> ResultAsync.wrap (fun deps ->
            deps.tryGetChat ()
            |> ResultAsync.bindAsync (function
                | Some chat -> consumerDeps |> callback chat
                | None -> deps |> Query.getCulturesCallback request.Value |> deps.sendResult))
