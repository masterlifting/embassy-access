[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Client

open System.Threading
open AIProvider.Services.Dependencies
open EA.Telegram.Dependencies

type Dependencies = {
    CT: CancellationToken
    Web: Web.Dependencies
    Culture: Culture.Dependencies
    Persistence: Persistence.Dependencies
}
