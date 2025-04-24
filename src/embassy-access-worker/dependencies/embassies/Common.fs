module internal EA.Worker.Dependencies.Embassies.Common

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open Worker.Domain

let inline private equalCountry (taskId: ActiveTaskId) (embassyId: EmbassyId) =
    let embassyCountry =
        embassyId.Value
        |> Graph.NodeId.split
        |> Seq.skip 1
        |> Seq.truncate 2
        |> Graph.NodeId.combine
    let taskCountry =
        taskId.Value
        |> Graph.NodeId.split
        |> Seq.skip 1
        |> Seq.truncate 2
        |> Graph.NodeId.combine
    embassyCountry = taskCountry

let getRequests (serviceId: ServiceId) (task: ActiveTask) =
    fun requestStorage ->
        requestStorage
        |> Storage.Request.Query.findManyWithServiceId serviceId
        |> ResultAsync.map (
            List.filter (fun request ->
                task.Id |> equalCountry <| request.Embassy.Id
                && request.UseBackground
                && (request.ProcessState <> InProcess
                    || request.ProcessState = InProcess
                       && request.Modified < DateTime.UtcNow.Subtract task.Duration))
        )
