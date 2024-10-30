[<RequireQualifiedAccess>]
module EA.Persistence.Storage

open EA.Domain
open Persistence.Domain

module FileSystem =
    open Persistence.Domain.FileSystem

    module Request =
        let create configuration =
            configuration
            |> Persistence.Storage.getConnectionString SectionName
            |> Result.bind (fun connectionString ->
                { Directory = connectionString
                  FileName = Key.Requests }
                |> Storage.Context.FileSystem
                |> Persistence.Storage.create)
