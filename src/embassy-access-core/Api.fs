[<RequireQualifiedAccess>]
module EmbassyAccess.Api

type ProcessRequestDeps = Russian of Embassies.Russian.Domain.ProcessRequestDeps
type SendNotificationDeps = Russian of Embassies.Russian.Domain.SendNotificationDeps

let processRequest deps request =
    match deps with
    | ProcessRequestDeps.Russian deps -> request |> Embassies.Russian.Core.processRequest deps

let sendNotification deps notification =
    match deps with
    | SendNotificationDeps.Russian deps -> notification |> Embassies.Russian.Notification.send deps
