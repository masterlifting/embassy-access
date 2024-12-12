[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Persistence

open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.Configuration
open EA.Core.Domain
open EA.Telegram.DataAccess
open EA.Core.DataAccess

type Dependencies =
    { initChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'>
      initServiceGraphStorage: string -> Result<ServiceGraph.ServiceGraphStorage, Error'>
      getEmbassyGraph: unit -> Async<Result<Graph.Node<EmbassyNode>, Error'>> }

    static member create cfg =
        let result = ResultBuilder()

        result {

            let! filePath = cfg |> Persistence.Storage.getConnectionString "FileSystem"

            let initializeChatStorage () =
                filePath |> Chat.FileSystem |> Chat.init

            let initializeRequestStorage () =
                filePath |> Request.FileSystem |> Request.init

            let getEmbassyGraph () =
                { SectionName = "Embassies"
                  Configuration = cfg }
                |> EmbassyGraph.Configuration
                |> EmbassyGraph.init
                |> ResultAsync.wrap EmbassyGraph.get

            let initServiceGraphStorage sectionName =
                { SectionName = sectionName
                  Configuration = cfg }
                |> ServiceGraph.Configuration
                |> ServiceGraph.init

            return
                { initChatStorage = initializeChatStorage
                  initRequestStorage = initializeRequestStorage
                  getEmbassyGraph = getEmbassyGraph
                  initServiceGraphStorage = initServiceGraphStorage }
        }
