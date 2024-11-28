[<RequireQualifiedAccess>]
module EA.Telegram.Persistence.Storage

open Persistence.Domain
open EA.Telegram.Domain

module FileSystem =
    open Persistence.Domain.FileSystem

    module Chat =
        let create configuration =
            configuration
            |> Persistence.Storage.getConnectionString SECTION_NAME
            |> Result.bind (fun filePath ->
                { FilePath = filePath
                  FileName = Key.CHATS_STORAGE_NAME }
                |> Connection.FileSystem
                |> Persistence.Storage.create)
