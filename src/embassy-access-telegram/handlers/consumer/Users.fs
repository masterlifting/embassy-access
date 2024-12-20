[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Consumer.Users

open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Routes.Users
open Infrastructure.Domain

let consume request =
    fun (deps: Core.Dependencies) ->
        match request with
        | Request.Get getRequest ->
            match getRequest with
            | GetRequest.Id id -> "" |> NotSupported |> Error |> async.Return
            | GetRequest.All -> "" |> NotSupported |> Error |> async.Return
            | GetRequest.Embassies(userId, embassies) -> "" |> NotSupported |> Error |> async.Return
