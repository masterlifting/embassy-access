[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Persistence

open Infrastructure.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess

module Russian =
    open EA.Russian.Services.Domain
    open EA.Russian.Services.DataAccess

    type Dependencies = {
        initKdmidRequestStorage: unit -> Result<Request.Storage<Kdmid.Payload, Kdmid.Payload.Entity>, Error'>
        initMidpassRequestStorage: unit -> Result<Request.Storage<Midpass.Payload, Midpass.Payload.Entity>, Error'>
    }

module Italian =
    open EA.Italian.Services.Domain
    open EA.Italian.Services.DataAccess

    type Dependencies = {
        initPrenotamiRequestStorage:
            unit -> Result<Request.Storage<Prenotami.Payload, Prenotami.Payload.Entity>, Error'>
    }

type Dependencies = {
    initChatStorage: unit -> Result<Chat.Storage, Error'>
    initServiceStorage: unit -> Result<ServiceGraph.Storage, Error'>
    initEmbassyStorage: unit -> Result<EmbassyGraph.Storage, Error'>
    RussianStorage: Russian.Dependencies
    ItalianStorage: Italian.Dependencies
}
