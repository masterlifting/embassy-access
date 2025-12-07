module internal EA.Worker.Initializer

open EA.Worker.Shared
open EA.Core.DataAccess

let run (_, _, _) =
    Postgre.Request.Migrations.apply Configuration.ENVIRONMENTS.PostgresConnection
