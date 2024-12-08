module EA.Telegram.Initializer

open Infrastructure
open Web.Telegram.Domain
open EA.Core.Domain

type PersistenceDeps =
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
                ("Embassies", cfg)
                |> EA.Core.DataAccess.EmbassyGraph.Configuration
                |> EA.Core.DataAccess.EmbassyGraph.init
                |> ResultAsync.wrap(EA.Core.DataAccess.EmbassyGraph.get)
                
            let getRussianServiceGraph () =
                ("RussianServices", cfg)
                |> EA.Core.DataAccess.ServiceGraph.Configuration
                |> EA.Core.DataAccess.ServiceGraph.init
                |> ResultAsync.wrap(EA.Core.DataAccess.ServiceGraph.get)

            return
                { initChatStorage = initializeChatStorage
                  initRequestStorage = initializeRequestStorage
                  getEmbassyGraph = getEmbassyGraph
                  getRussianServiceGraph = getRussianServiceGraph }
        }

type ConsumerDeps =
    { ChatId: ChatId
      MessageId: int
      Configuration: Microsoft.Extensions.Configuration.IConfigurationRoot
      CancellationToken: System.Threading.CancellationToken
      Persistence: PersistenceDeps }

    static member create (dto: Consumer.Dto<_>) cfg ct =
        let result = ResultBuilder()

        result {
            let! persistence = PersistenceDeps.create cfg

            return
                { ChatId = dto.ChatId
                  MessageId = dto.Id
                  Configuration = cfg
                  CancellationToken = ct
                  Persistence = persistence }
        }
