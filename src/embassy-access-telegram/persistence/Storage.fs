﻿[<RequireQualifiedAccess>]
module EA.Telegram.Persistence.Storage

open Persistence.Domain

module FileSystem =
    open Persistence.Domain.FileSystem

    module Chat =
        let create configuration =
            configuration
            |> Persistence.Storage.getConnectionString SectionName
            |> Result.bind (fun connectionString ->
                { Directory = connectionString
                  FileName = EA.Telegram.Domain.Key.Chats }
                |> Storage.Context.FileSystem
                |> Persistence.Storage.create)
