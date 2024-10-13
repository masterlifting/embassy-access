module internal EmbassyAccess.Worker.Initializer

open Infrastructure
open Persistence
open Persistence.Domain
open Worker.Domain

let initialize () =
    fun (configuration, ct) ->
        configuration
        |> Storage.getConnectionString FileSystem.SectionName
        |> ResultAsync.wrap (Temporary.createTestData ct)
        |> ResultAsync.map (fun data -> $"Test data has been created. Count: {data.Length}. ")
        |> ResultAsync.map (fun msg -> Success(msg + $" {Settings.AppName} has been initialized."))
    |> Some
