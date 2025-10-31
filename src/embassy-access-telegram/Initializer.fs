module EA.Telegram.Initializer

open EA.Telegram.Dependencies
open EA.Telegram.DataAccess

let run () =
    Postgre.Chat.Migrations.apply Configuration.ENVIRONMENTS.PostgresConnection
