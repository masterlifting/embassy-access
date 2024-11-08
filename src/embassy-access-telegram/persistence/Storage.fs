[<RequireQualifiedAccess>]
module EA.Telegram.Persistence.Storage

open Persistence.Domain

module FileSystem =
    open Persistence.Domain.FileSystem

    module Chat =
        let create configuration =
            configuration
            |> Persistence.Storage.getConnectionString SECTION_NAME
            |> Result.bind (fun connectionString ->
                { Directory = connectionString
                  FileName = EA.Telegram.Domain.Key.CHATS_TABLE_NAME }
                |> Storage.Context.FileSystem
                |> Persistence.Storage.create)
