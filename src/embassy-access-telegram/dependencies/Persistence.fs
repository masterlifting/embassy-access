[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Persistence

open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.Configuration
open EA.Core.Domain
open EA.Telegram.DataAccess
open EA.Core.DataAccess

type Dependencies =
    { initChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'>
      initServiceGraphStorage: unit -> Result<ServiceGraph.ServiceGraphStorage, Error'>
      initEmbassyGraphStorage: unit -> Result<EmbassyGraph.EmbassyGraphStorage, Error'> }

    static member create cfg =
        let result = ResultBuilder()

        result {

            let! filePath =
                cfg
                |> Configuration.getSection<string> "Persistence:FileSystem"
                |> Option.map Ok
                |> Option.defaultValue ("Section 'Persistence:FileSystem' in the configuration." |> NotFound |> Error)

            let initChatStorage () =
                filePath |> Chat.FileSystem |> Chat.init

            let initRequestStorage () =
                filePath |> Request.FileSystem |> Request.init

            let initEmbassyGraphStorage () =
                { SectionName = "Embassies"
                  Configuration = cfg }
                |> EmbassyGraph.Configuration
                |> EmbassyGraph.init

            let initServiceGraphStorage () =
                { SectionName = "Services"
                  Configuration = cfg }
                |> ServiceGraph.Configuration
                |> ServiceGraph.init

            return
                { initChatStorage = initChatStorage
                  initRequestStorage = initRequestStorage
                  initEmbassyGraphStorage = initEmbassyGraphStorage
                  initServiceGraphStorage = initServiceGraphStorage }
        }
