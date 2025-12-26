module EA.Telegram.Initializer

open Infrastructure.Prelude
open AIProvider.Features.DataAccess
open EA.Core.DataAccess
open EA.Telegram.Shared
open EA.Telegram.DataAccess

let private resultAsync = ResultAsyncBuilder()
let run () =
    let cs = Configuration.ENVIRONMENTS.PostgresConnection
    resultAsync {
        do! Postgre.Culture.Migrations.apply cs
        do! Postgre.Request.Migrations.apply cs
        return Postgre.Chat.Migrations.apply cs
    }
