[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Subscriptions

open Infrastructure.Domain
open EA.Telegram.Dependencies

let respond request chat =
    fun (deps: Request.Dependencies) -> "Subscriptions is not implemented." |> NotImplemented |> Error |> async.Return
