module internal EA.Worker.Dependencies.Embassies.Russian

open System
open EA.Worker.Domain.Embassies.Russian
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Worker.Dependencies
open Worker.Domain
open EA.Russian.Clients
open EA.Russian.Clients.Domain.Kdmid

module Kdmid =
    open Infrastructure.Logging
    open EA.Telegram.Services.Embassies.Russian.Kdmid
    open EA.Telegram.Dependencies.Embassies.Russian

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

                let notificationDeps: Kdmid.Notification.Dependencies = {
                    translateMessages = telegram.Culture.translateSeq
                    setRequestAppointments = telegram.Persistence.setRequestAppointments
                    getRequestChats = telegram.Persistence.getRequestChats
                    sendMessages = telegram.Web.Telegram.sendMessages
                }

                let notify notification =
                    notificationDeps
                    |> Message.Notification.spread notification
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
                    persistence.RequestStorage
                    |> Request.Query.findManyByPartServiceId partServiceId
                    |> ResultAsync.map (
                        List.filter (fun request ->
                            equalCountry request.Service.Embassy.Id
                            && request.IsBackground
                            && (request.ProcessState <> InProcess
                                || request.ProcessState = InProcess
                                   && request.Modified < DateTime.UtcNow.Subtract task.Duration))
                    )

                let tryProcessFirst requests =
                    {
                        CancellationToken = ct
                        RequestStorage = persistence.RequestStorage
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
