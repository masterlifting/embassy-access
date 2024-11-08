[<RequireQualifiedAccess>]
module EA.Persistence.Storage

open EA.Core.Domain
open Persistence.Domain

module FileSystem =
    open Persistence.Domain.FileSystem

    module Request =
        let create configuration =
            configuration
            |> Persistence.Storage.getConnectionString SECTION_NAME
            |> Result.bind (fun connectionString ->
                { Directory = connectionString
                  FileName = Key.REQUESTS_TABLE_NAME }
                |> Storage.Context.FileSystem
                |> Persistence.Storage.create)
