[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Persistence

open System
open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.Storages
open Persistence.Storages.Domain
open Worker.Domain
open AIProvider.Services.DataAccess
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess

let inline private equalCountry (embassyId: Graph.NodeId) (taskId: Graph.NodeId) =
    let embassyCountry =
        embassyId
        |> Graph.NodeId.split
        |> Seq.skip 1
        |> Seq.truncate 2
        |> Graph.NodeId.combine
    let taskCountry =
        taskId
        |> Graph.NodeId.split
        |> Seq.skip 1
        |> Seq.truncate 2
        |> Graph.NodeId.combine
    embassyCountry = taskCountry

type Dependencies = {
    ChatStorage: Chat.ChatStorage
    RequestStorage: Request.RequestStorage
    initCultureStorage: unit -> Result<Culture.Response.Storage, Error'>
    initServiceGraphStorage: unit -> Result<ServiceGraph.ServiceGraphStorage, Error'>
    initEmbassyGraphStorage: unit -> Result<EmbassyGraph.EmbassyGraphStorage, Error'>
    getRequests: Graph.NodeId * ActiveTask -> Async<Result<Request list, Error'>>
    resetData: unit -> Async<Result<unit, Error'>>
} with

    static member create cfg =
        let result = ResultBuilder()

        result {

            let! fileStoragePath =
                cfg
                |> Configuration.Client.tryGetSection<string> "Persistence:FileSystem"
                |> Option.map Ok
                |> Option.defaultValue (
                    "The configuration section 'Persistence:FileSystem' not found."
                    |> NotFound
                    |> Error
                )

            let initCultureStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "Culture.json"
                }
                |> Culture.Response.FileSystem
                |> Culture.Response.init

            let initChatStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "Chats.json"
                }
                |> Chat.FileSystem
                |> Chat.init

            let initRequestStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "Requests.json"
                }
                |> Request.FileSystem
                |> Request.init

            let initEmbassyGraphStorage () =
                {
                    Configuration.Connection.Section = "Embassies"
                    Configuration.Connection.Provider = cfg
                }
                |> EmbassyGraph.Configuration
                |> EmbassyGraph.init

            let initServiceGraphStorage () =
                {
                    Configuration.Connection.Section = "Services"
                    Configuration.Connection.Provider = cfg
                }
                |> ServiceGraph.Configuration
                |> ServiceGraph.init

            let! chatStorage = initChatStorage ()
            let! requestStorage = initRequestStorage ()

            let getRequests (partServiceId, task: ActiveTask) =
                requestStorage
                |> Request.Query.findManyByPartServiceId partServiceId
                |> ResultAsync.map (
                    List.filter (fun request ->
                        equalCountry request.Service.Embassy.Id task.Id
                        && request.IsBackground
                        && (request.ProcessState <> InProcess
                            || request.ProcessState = InProcess
                               && request.Modified < DateTime.UtcNow.Subtract task.Duration))
                )

            let resetData () =
                let resultAsync = ResultAsyncBuilder()

                resultAsync {

                    let! subscriptions = chatStorage |> Chat.Query.getSubscriptions |> ResultAsync.map Set.ofSeq

                    let! requestIdentifiers =
                        requestStorage |> Request.Query.getIdentifiers |> ResultAsync.map Set.ofSeq

                    let existingData = subscriptions |> Set.intersect <| requestIdentifiers
                    let subscriptionsToRemove = existingData |> Set.difference subscriptions
                    let requestIdsToRemove = existingData |> Set.difference requestIdentifiers

                    do! chatStorage |> Chat.Command.deleteSubscriptions subscriptionsToRemove
                    return requestStorage |> Request.Command.deleteMany requestIdsToRemove
                }

            return {
                ChatStorage = chatStorage
                RequestStorage = requestStorage
                initCultureStorage = initCultureStorage
                initEmbassyGraphStorage = initEmbassyGraphStorage
                initServiceGraphStorage = initServiceGraphStorage
                getRequests = getRequests
                resetData = resetData
            }
        }
