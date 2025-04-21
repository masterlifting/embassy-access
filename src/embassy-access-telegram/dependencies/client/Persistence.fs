[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Persistence

open EA.Core.Domain
open EA.Telegram.Domain
open Infrastructure.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess

module Russian =
    open EA.Russian.Services.Domain

    type Dependencies = {
        initKdmidRequestStorage: unit -> Result<Request.Storage<Kdmid.Payload>, Error'>
        initMidpassRequestStorage: unit -> Result<Request.Storage<Midpass.Payload>, Error'>
    }

module Italian =
    open EA.Italian.Services.Domain

    type Dependencies = {
        initPrenotamiRequestStorage: unit -> Result<Request.Storage<Prenotami.Payload>, Error'>
    }

type Dependencies = {
    initChatStorage: unit -> Result<Chat.Storage, Error'>
    initServiceStorage: unit -> Result<ServiceGraph.Storage, Error'>
    initEmbassyStorage: unit -> Result<EmbassyGraph.Storage, Error'>
    RussianStorage: Russian.Dependencies
    ItalianStorage: Italian.Dependencies
}
