[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Client

open System.Threading
open EA.Telegram.Dependencies

type Dependencies = {
    ct: CancellationToken
    Web: Web.Dependencies
    Culture: Culture.Dependencies
    Persistence: Persistence.Dependencies
}
