[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer

open Web.Telegram.Domain

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      Configuration: Microsoft.Extensions.Configuration.IConfigurationRoot
      CancellationToken: System.Threading.CancellationToken
      Persistence: Persistence.Dependencies }

    static member create (dto: Consumer.Dto<_>) cfg ct (persistenceDeps: Persistence.Dependencies) =
        { ChatId = dto.ChatId
          MessageId = dto.Id
          Configuration = cfg
          CancellationToken = ct
          Persistence = persistenceDeps }
