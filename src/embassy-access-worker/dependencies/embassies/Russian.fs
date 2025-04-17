module internal EA.Worker.Dependencies.Embassies.Russian

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Services.Embassies
open EA.Telegram.Dependencies
open EA.Worker.Dependencies
open EA.Russian.Services

module Kdmid =
    open EA.Russian.Services.Domain.Kdmid

    type Dependencies = {
        TaskName: string
        getRequests: Graph.NodeId -> Async<Result<Request list, Error'>>
        tryProcessFirst: Request list -> Async<Result<Request, Error' list>>
    } with

        static member create (task: ActiveTask) cfg ct =
            let result = ResultBuilder()

            result {
                let! persistence = Persistence.Dependencies.create cfg
                let! telegram = Telegram.Dependencies.create cfg ct

                let notificationDeps: Notification.Dependencies = {
                    printPayload = Credentials.create >> Result.map Credentials.print
                    translateMessages = telegram.Culture.translateSeq
                    setRequestAppointments = telegram.Persistence.setRequestAppointments
                    getRequestChats = telegram.Persistence.getRequestChats
                    sendMessages = telegram.Web.Telegram.sendMessages
                }

                let notify notification =
                    notificationDeps
                    |> Notification.spread notification
                    |> ResultAsync.mapError (_.Message >> Log.crt)
                    |> Async.Ignore

                let inline equalCountry (embassyId: Graph.NodeId) =
                    let embassyCountry =
                        embassyId
                        |> Graph.NodeId.split
                        |> Seq.skip 1
                        |> Seq.truncate 2
                        |> Graph.NodeId.combine
                    let taskCountry =
                        task.Id
                        |> Graph.NodeId.split
                        |> Seq.skip 1
                        |> Seq.truncate 2
                        |> Graph.NodeId.combine
                    embassyCountry = taskCountry

                let getRequests partServiceId =
                    persistence.RussianRequestsStorage
                    |> Request.Query.findManyByPartServiceId partServiceId
                    |> ResultAsync.map (
                        List.filter (fun request ->
                            equalCountry request.Service.Embassy.Id
                            && request.UseBackground
                            && (request.ProcessState <> InProcess
                                || request.ProcessState = InProcess
                                   && request.Modified < DateTime.UtcNow.Subtract task.Duration))
                    )

                let tryProcessFirst requests =
                    {
                        CancellationToken = ct
                        RequestStorage = persistence.RussianRequestsStorage
                    }
                    |> Kdmid.Client.init
                    |> Result.map (fun client -> client, notify)
                    |> ResultAsync.wrap (Kdmid.Service.tryProcessFirst requests)

                return {
                    TaskName = ActiveTask.print task
                    getRequests = getRequests
                    tryProcessFirst = tryProcessFirst
                }
            }
