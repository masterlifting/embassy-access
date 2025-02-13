module EA.Telegram.Services.Consumer.Culture.Command

open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Endpoints.Culture.Post.Model
open Infrastructure.Domain

let setCulture (model: Culture) =
    fun (deps: Culture.Dependencies) -> "setCulture" |> NotSupported |> Error |> async.Return