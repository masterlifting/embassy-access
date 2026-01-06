module internal EA.Worker.Shared.Embassies

open System
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess

let getRequests (serviceId: ServiceId) (taskDuration: TimeSpan) =

    fun (requestStorage, hasRequiredService) ->
        requestStorage
        |> Storage.Request.Query.findMany (Storage.Request.Query.StartWithServiceId serviceId)
        |> ResultAsync.map (
            List.filter (fun request ->
                request.Service.Id |> ServiceId |> hasRequiredService
                && (request.ProcessState <> InProcess
                    || request.ProcessState = InProcess
                       && request.Modified < DateTime.UtcNow.Subtract taskDuration))
        )
