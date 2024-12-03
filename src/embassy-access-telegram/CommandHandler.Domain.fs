module EA.Telegram.CommandHandler.CommandHandler.Domain

open Infrastructure
open EA.Core.Domain
open EA.Telegram.Domain
open Web.Telegram.Domain

type GetUserEmbassies =
    { initializeChatStorage: unit -> Result<Persistence.Domain.Storage, Error'>
      getChat: ChatId -> Persistence.Domain.Storage -> Async<Result<Chat option, Error'>>
      getChatEmbassies: Chat -> Async<Result<Set<Embassy>, Error'>> }

    member this.create cfg ct =
        let result = ResultBuilder()

        result {
            let fileSystemSection = Persistence.Domain.FileSystem.SECTION_NAME

            let! fileSystemConnectionString = cfg |> Persistence.Storage.getConnectionString fileSystemSection

            let initializeChatStorage () =
                fileSystemConnectionString
                |> EA.Telegram.DataAccess.Chat.FileSystem
                |> EA.Telegram.DataAccess.Chat.initialize

            return
                { initializeChatStorage = initializeChatStorage
                  getChat = EA.Telegram.DataAccess.Chat.tryFindById
                  getChatEmbassies = fun _ -> async.Return(Ok Set.empty) }
        }
