[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Persistence

open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.Configuration
open EA.Core.Domain

type Dependencies =
    { initChatStorage: unit -> Result<EA.Telegram.DataAccess.Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<EA.Core.DataAccess.Request.RequestStorage, Error'>
      getEmbassyGraph: unit -> Async<Result<Graph.Node<EmbassyGraph>, Error'>>
      getRussianServiceGraph: unit -> Async<Result<Graph.Node<ServiceGraph>, Error'>> }

    static member create cfg =
        let result = ResultBuilder()

        result {

            let! filePath = cfg |> Persistence.Storage.getConnectionString "FileSystem"

            let initializeChatStorage () =
                filePath
                |> EA.Telegram.DataAccess.Chat.FileSystem
                |> EA.Telegram.DataAccess.Chat.init

            let initializeRequestStorage () =
                filePath
                |> EA.Core.DataAccess.Request.FileSystem
                |> EA.Core.DataAccess.Request.init

            let getEmbassyGraph () =
                { SectionName = "Embassies"
                  Configuration = cfg }
                |> EA.Core.DataAccess.EmbassyGraph.Configuration
                |> EA.Core.DataAccess.EmbassyGraph.init
                |> ResultAsync.wrap EA.Core.DataAccess.EmbassyGraph.get

            let getRussianServiceGraph () =
                { SectionName = "RussianServices"
                  Configuration = cfg }
                |> EA.Core.DataAccess.ServiceGraph.Configuration
                |> EA.Core.DataAccess.ServiceGraph.init
                |> ResultAsync.wrap EA.Core.DataAccess.ServiceGraph.get

            return
                { initChatStorage = initializeChatStorage
                  initRequestStorage = initializeRequestStorage
                  getEmbassyGraph = getEmbassyGraph
                  getRussianServiceGraph = getRussianServiceGraph }
        }
