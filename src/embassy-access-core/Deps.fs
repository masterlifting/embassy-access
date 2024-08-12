[<RequireQualifiedAccess>]
module EmbassyAccess.Deps

module Russian =
    let getAppointments (storage, config, ct) =
        Embassies.Russian.Deps.createGetAppointmentsDeps ct config storage
        |> Api.GetAppointmentsDeps.Russian

    let bookAppointment (storage, config, ct) =
        Embassies.Russian.Deps.createBookAppointmentDeps ct config storage
        |> Api.BookAppointmentDeps.Russian
