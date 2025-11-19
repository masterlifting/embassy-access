[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Client

open System.Threading
open Infrastructure.Prelude
open AIProvider.Services.Dependencies
open AIProvider.Clients
open Web.Clients
open EA.Telegram.Dependencies

let private result = ResultBuilder()

type Dependencies = {
    ct: CancellationToken
    Web: Web.Dependencies
    Culture: Culture.Dependencies
    Persistence: Persistence.Dependencies
} with

    static member create cfg ct =
        result {
            let! telegramClient =
                Telegram.Client.init {
                    Token = Configuration.ENVIRONMENTS.TelegramBotToken
                }

            let! openApiClient =
                OpenAI.Client.init {
                    Token = Configuration.ENVIRONMENTS.OpenAIKey
                    ProjectId = "proj_OsfEwmtR7Shm2Uj4wqJTdgcC"
                }

            let! persistenceDeps = Persistence.Dependencies.create cfg

            let webDeps = telegramClient |> Web.Dependencies.create ct

            let! cultureDeps =
                persistenceDeps.initCultureStorage ()
                |> Result.map (fun storage ->
                    Culture.Dependencies.create ct {
                        Provider = AIProvider.Client.OpenAI openApiClient
                        Storage = storage
                    })

            return {
                ct = ct
                Web = webDeps
                Culture = cultureDeps
                Persistence = persistenceDeps
            }
        }
