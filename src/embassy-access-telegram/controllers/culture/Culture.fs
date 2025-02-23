[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Culture.Culture

open Infrastructure.Domain
open Infrastructure.Prelude
open Multilang
open Multilang.Domain
open Web.Telegram.Domain.Producer
open EA.Telegram.Endpoints.Culture
open EA.Telegram.Endpoints.Culture.Request
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Services.Consumer.Culture

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

let wrap (request: EA.Telegram.Endpoints.Request.Request) callback =
    fun (consumerDeps: Consumer.Dependencies) ->
        Culture.Dependencies.create consumerDeps
        |> ResultAsync.wrap (fun deps ->
            deps.tryGetChat ()
            |> ResultAsync.bindAsync (function
                | Some chat -> consumerDeps |> callback chat
                | None -> deps |> Query.getCulturesCallback request.Value |> deps.sendResult))

let apply (culture: Culture) (msgRes: Async<Result<Data, Error'>>) =
    fun (deps: Consumer.Dependencies) ->
        let resultAsync = ResultAsyncBuilder()
        resultAsync{
            let! msg = msgRes
            
            return
                match msg with
                | Text dto -> "Culture.apply.Text" |> NotSupported |> Error |> async.Return
                | Html dto -> "Culture.apply.Html" |> NotSupported |> Error |> async.Return
                | Buttons dto -> "Culture.apply.Buttons" |> NotSupported |> Error |> async.Return
                | WebApps dto -> "Culture.apply.WebApp" |> NotSupported |> Error |> async.Return
        }
            
