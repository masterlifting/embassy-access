module internal EA.Worker.Dependencies.Embassies.Common

open System
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess

let getRequests (rootServiceId: ServiceId) (taskDuration: TimeSpan) =
    fun (requestStorage, hasRequiredService) ->
        requestStorage
        |> Storage.Request.Query.findMany (Storage.Request.Query.ContainsServiceId rootServiceId)
        |> ResultAsync.map (
            List.filter (fun request ->
                request.Service.Id |> hasRequiredService
                && (request.ProcessState <> InProcess
                    || request.ProcessState = InProcess
                       && request.Modified < DateTime.UtcNow.Subtract taskDuration))
        )
