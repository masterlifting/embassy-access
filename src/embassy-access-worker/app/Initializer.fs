module internal EA.Worker.Initializer

open EA.Worker.Dependencies
open EA.Core.DataAccess
open Worker.Domain

let run (_: ActiveTask, _: EA.Worker.Dependencies.WorkerTask.Dependencies, _) =
    Postgre.Request.Migrations.apply Configuration.ENVIRONMENTS.PostgresConnection
