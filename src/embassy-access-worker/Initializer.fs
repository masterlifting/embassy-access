module internal EmbassyAccess.Worker.Initializer

open Infrastructure
open Worker.Domain

let initialize () =
    fun (_, ct) ->
        Temporary.createTestData ct
        |> ResultAsync.map (fun data -> $"Test data has been created. Count: {data.Length}. ")
        |> ResultAsync.map (fun msg -> Success(msg + $" {Settings.AppName} has been initialized."))
    |> Some
