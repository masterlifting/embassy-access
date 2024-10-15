[<RequireQualifiedAccess>]
module EA.Telegram.Persistence.Storage

open EA.Domain
open Persistence.Domain
open Persistence.Domain.FileSystem

module Chat =
    let create configuration =
        configuration
        |> Persistence.Storage.getConnectionString FileSystem.SectionName
        |> Result.bind (fun connectionString ->
            { Directory = connectionString
              FileName = EA.Telegram.Domain.Key.Chats }
            |> Storage.Context.FileSystem
            |> Persistence.Storage.create)

module Request =
    let create configuration =
        configuration
        |> Persistence.Storage.getConnectionString FileSystem.SectionName
        |> Result.bind (fun connectionString ->
            { Directory = connectionString
              FileName = Key.Requests }
            |> Storage.Context.FileSystem
            |> Persistence.Storage.create)
