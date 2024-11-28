﻿[<RequireQualifiedAccess>]
module EA.Core.Persistence.Storage

open EA.Core.Domain
open Persistence.Domain

module FileSystem =
    open Persistence.Domain.FileSystem

    module Request =
        let create configuration =
            configuration
            |> Persistence.Storage.getConnectionString SECTION_NAME
            |> Result.bind (fun filePath ->
                { FilePath = filePath
                  FileName = Constants.REQUESTS_STORAGE_NAME }
                |> Connection.FileSystem
                |> Persistence.Storage.create)
