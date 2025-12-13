module EA.Telegram.Initializer

open Infrastructure.Prelude
open AIProvider.Services.DataAccess
open EA.Core.DataAccess
open EA.Telegram.DataAccess

let private resultAsync = ResultAsyncBuilder()
let run () =
    let pgConnectionString =
        EA.Telegram.Shared.Configuration.ENVIRONMENTS.PostgresConnection
    resultAsync {
        do! Postgre.Culture.Migrations.apply pgConnectionString
        do! Postgre.Request.Migrations.apply pgConnectionString
        return Postgre.Chat.Migrations.apply pgConnectionString
    }
