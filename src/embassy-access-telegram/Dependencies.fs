module EA.Telegram.Dependencies

open Infrastructure
open Web.Telegram.Domain

type PersistenceDeps =
    { initChatStorage: unit -> Result<EA.Telegram.DataAccess.Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<EA.Core.DataAccess.Request.RequestStorage, Error'>
      getEmbassiesGraph: unit -> Async<Result<Graph.Node<EA.Core.Domain.Embassy>, Error'>>
      getServiceInfoGraph: unit -> Async<Result<Graph.Node<EA.Embassies.Russian.Domain.ServiceInfo>, Error'>> }

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

            let initializeEmbassiesGraph () =
                cfg |> EA.Core.Settings.Embassy.getGraph

            let initializeServiceInfoGraph () =
                cfg |> EA.Embassies.Russian.Settings.ServiceInfo.getGraph

            return
                { initChatStorage = initializeChatStorage
                  initRequestStorage = initializeRequestStorage
                  getEmbassiesGraph = initializeEmbassiesGraph
                  getServiceInfoGraph = initializeServiceInfoGraph }
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
