module internal EA.Worker.Dependencies.Embassies.Common

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open Web.Clients.Domain

let getRequests (embassyId: EmbassyId) (serviceId: ServiceId) =
    fun requestStorage ->
        requestStorage
        |> Storage.Request.Query.findMany (Storage.Request.Query.ByEmbassyAndServiceId(embassyId, serviceId))

let getRequestsToProcess (rootServiceId: ServiceId) (taskDuration: TimeSpan) =
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

let spreadTranslatedMessages (data: (Culture * Telegram.Producer.Message) seq) =
    fun
        (translateMessages:
            Culture -> Telegram.Producer.Message seq -> Async<Result<Telegram.Producer.Message list, Error'>>,
         sendMessages: Telegram.Producer.Message seq -> Async<Result<unit, Error'>>) ->
        data
        |> Seq.groupBy fst
        |> Seq.map (fun (culture, group) -> culture, group |> Seq.map snd |> List.ofSeq)
        |> Seq.map (fun (culture, group) -> translateMessages culture group)
        |> fun asyncResults -> asyncResults |> Async.Parallel |> Async.map Result.choose
        |> ResultAsync.map (Seq.collect id)
        |> ResultAsync.bindAsync sendMessages
