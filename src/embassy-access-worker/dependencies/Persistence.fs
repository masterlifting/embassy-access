[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Persistence

open System
open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.Storages
open Persistence.Storages.Domain
open AIProvider.Services.DataAccess
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess
open EA.Russian.Services.Domain
open EA.Italian.Services.Domain

type Dependencies = {
    initChatStorage: unit -> Result<Chat.Storage, Error'>
    initServiceStorage: unit -> Result<ServiceGraph.Storage, Error'>
    initEmbassyStorage: unit -> Result<EmbassyGraph.Storage, Error'>
    initRussianKdmidRequestStorage: unit -> Result<Request.Storage<Kdmid.Payload>, Error'>
    initItalianPrenotamiRequestStorage: unit -> Result<Request.Storage<Prenotami.Payload>, Error'>
    initCultureStorage: unit -> Result<Culture.Response.Storage, Error'>
    resetData: unit -> Async<Result<unit, Error'>>
} with

    static member create cfg =
        let result = ResultBuilder()

        result {

            let! fileStoragePath =
                cfg
                |> Configuration.Client.tryGetSection<string> "Persistence:FileSystem"
                |> Option.map Ok
                |> Option.defaultValue (
                    "The configuration section 'Persistence:FileSystem' not found."
                    |> NotFound
                    |> Error
                )

            let! appKey =
                cfg
                |> Configuration.Client.tryGetSection<string> "AppKey"
                |> Option.map Ok
                |> Option.defaultValue ("The configuration section 'AppKey' not found." |> NotFound |> Error)

            let initCultureStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "Culture.json"
                }
                |> Culture.Response.FileSystem
                |> Culture.Response.init

            let initChatStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "Chats.json"
                }
                |> Storage.Chat.FileSystem
                |> Storage.Chat.init

            let initRussianKdmidRequestStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "Requests.Rus.Kdmid.json"
                }
                |> Storage.Request.FileSystem
                |> Storage.Request.init (Kdmid.Payload.serialize, Kdmid.Payload.deserialize)

            let initItalianPrenotamiRequestStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "Requests.Ita.Prenotami.json"
                }
                |> Storage.Request.FileSystem
                |> Storage.Request.init (Prenotami.Payload.serialize appKey, Prenotami.Payload.deserialize appKey)

            let initEmbassyStorage () =
                {
                    Configuration.Connection.Section = "Embassies"
                    Configuration.Connection.Provider = cfg
                }
                |> EmbassyGraph.Configuration
                |> EmbassyGraph.init

            let initServiceStorage () =
                {
                    Configuration.Connection.Section = "Services"
                    Configuration.Connection.Provider = cfg
                }
                |> ServiceGraph.Configuration
                |> ServiceGraph.init

            let! chatStorage = initChatStorage ()
            let! russianKdmidRequestStorage = initRussianKdmidRequestStorage ()
            let! italianPrenotamiRequestStorage = initItalianPrenotamiRequestStorage ()

            let resetData () =
                let resultAsync = ResultAsyncBuilder()

                resultAsync {
                    let! subscriptions = chatStorage |> Storage.Chat.Query.getSubscriptions |> ResultAsync.map Set.ofSeq

                    let! russianKdmidRequestIdentifiers =
                        russianKdmidRequestStorage
                        |> Storage.Request.Query.getIdentifiers
                        |> ResultAsync.map Set.ofSeq

                    let! italianPrenotamiRequestIdentifiers =
                        italianPrenotamiRequestStorage
                        |> Storage.Request.Query.getIdentifiers
                        |> ResultAsync.map Set.ofSeq

                    // All valid request IDs
                    let allRequestIdentifiers =
                        Set.union russianKdmidRequestIdentifiers italianPrenotamiRequestIdentifiers

                    // Subscriptions that reference nonexistent requests
                    let subscriptionsToRemove = Set.difference subscriptions allRequestIdentifiers

                    // Requests that aren't referenced by any subscription
                    let russianKdmidRequestIdsToRemove =
                        Set.difference russianKdmidRequestIdentifiers subscriptions
                    let italianPrenotamiRequestIdsToRemove =
                        Set.difference italianPrenotamiRequestIdentifiers subscriptions

                    // Delete all invalid subscriptions in one operation
                    do! chatStorage |> Storage.Chat.Command.deleteSubscriptions subscriptionsToRemove

                    // Delete invalid requests
                    do!
                        russianKdmidRequestStorage
                        |> Storage.Request.Command.deleteMany russianKdmidRequestIdsToRemove

                    return
                        italianPrenotamiRequestStorage
                        |> Storage.Request.Command.deleteMany italianPrenotamiRequestIdsToRemove
                }

            return {
                initServiceStorage = initServiceStorage
                initEmbassyStorage = initEmbassyStorage
                initCultureStorage = initCultureStorage
                initChatStorage = fun () -> chatStorage |> Ok
                initRussianKdmidRequestStorage = fun () -> russianKdmidRequestStorage |> Ok
                initItalianPrenotamiRequestStorage = fun () -> italianPrenotamiRequestStorage |> Ok
                resetData = resetData
            }
        }
