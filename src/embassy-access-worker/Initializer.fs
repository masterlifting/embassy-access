module internal EmbassyAccess.Worker.Initializer

open Worker.Domain

let initialize () =
    fun (_, _) -> Settings.AppName + " has been initialized." |> Info |> Ok |> async.Return
    |> Some
