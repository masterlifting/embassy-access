[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Consumer.Users

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Routes.Users

let consume request =
    fun (deps: Core.Dependencies) ->
        Users.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Request.Get getRequest ->
                match getRequest with
                | GetRequest.Id id -> "" |> NotSupported |> Error |> async.Return
                | GetRequest.All -> "" |> NotSupported |> Error |> async.Return
                | GetRequest.Embassies(userId, embassies) -> "" |> NotSupported |> Error |> async.Return)
