module internal EA.Worker.Dependencies.Embassies.Common

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open Web.Clients.Domain
open EA.Telegram.DataAccess
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
        |> Storage.Request.Query.findMany (Storage.Request.Query.WithServiceId serviceId)
        |> ResultAsync.map (
            List.filter (fun request ->
                task.Id |> equalCountry <| request.Embassy.Id
                && request.AutoProcessing
                && (request.ProcessState <> InProcess
                    || request.ProcessState = InProcess
                       && request.Modified < DateTime.UtcNow.Subtract task.Duration))
        )

let getRequestChats request =
    fun (requestStorage, chatStorage) ->
        requestStorage
        |> Storage.Request.Query.findMany (
            Storage.Request.Query.ByEmbassyAndServiceId(request.Embassy.Id, request.Service.Id)
        )
        |> ResultAsync.map (Seq.map _.Id)
        |> ResultAsync.bindAsync (fun subscriptionIds ->
            chatStorage |> Storage.Chat.Query.findManyBySubscriptions subscriptionIds)

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
