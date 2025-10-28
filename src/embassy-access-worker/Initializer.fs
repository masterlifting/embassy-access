module internal EA.Worker.Initializer

open EA.Worker.Dependencies
open EA.Core.DataAccess

let run (_, __, ___) =
    Postgre.Request.Migrations.apply Configuration.ENVIRONMENTS.PostgresConnection
