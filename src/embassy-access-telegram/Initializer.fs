module EA.Telegram.Initializer

open Infrastructure.Prelude
open AIProvider.Services.DataAccess
open EA.Telegram.Dependencies
open EA.Telegram.DataAccess

let private resultAsync = ResultAsyncBuilder()
let run () =
    let pgConnectionString = Configuration.ENVIRONMENTS.PostgresConnection
    resultAsync {
        do! Postgre.Culture.Migrations.apply pgConnectionString
        return Postgre.Chat.Migrations.apply pgConnectionString
    }
