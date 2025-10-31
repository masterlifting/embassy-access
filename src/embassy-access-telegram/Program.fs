open System.Threading
open Infrastructure
open Infrastructure.Prelude
open Infrastructure.Configuration.Domain
open EA.Telegram.Dependencies
open AIProvider.Services.Dependencies

let private resultAsync = ResultAsyncBuilder()

[<EntryPoint>]
let main _ =

    Logging.Client.getLevel () |> Logging.Client.Console |> Logging.Client.init

    let run () =
        resultAsync {
            let! configuration =
                { Files = [ "appsettings.yaml" ] }
                |> Configuration.Client.Yaml
                |> Configuration.Client.init
                |> async.Return

            let version =
                configuration
                |> Configuration.Client.getSection<string> "Version"
                |> Option.defaultValue "unknown"

            Logging.Log.inf $"EA.Telegram version: %s{version}"

            let ct = CancellationToken.None

            let! telegramClient =
                Web.Clients.Telegram.Client.init {
                    Token = Configuration.ENVIRONMENTS.TelegramBotToken
                }
                |> async.Return

            let! openApiClient =
                AIProvider.Clients.OpenAI.Client.init {
                    Token = Configuration.ENVIRONMENTS.OpenAIApiKey
                    ProjectId = "embassy-access"
                }
                |> async.Return

            let! persistenceDeps = Persistence.Dependencies.create configuration |> async.Return

            let! cultureStorage = persistenceDeps.initCultureStorage () |> async.Return

            let webDeps = telegramClient |> Web.Dependencies.create ct

            let cultureDeps =
                Culture.Dependencies.create ct {
                    Culture.Provider = AIProvider.Client.OpenAI openApiClient
                    Culture.Storage = cultureStorage
                }

            let deps: EA.Telegram.Dependencies.Client.Dependencies = {
                ct = ct
                Web = webDeps
                Culture = cultureDeps
                Persistence = persistenceDeps
            }

            return EA.Telegram.Client.start deps
        }

    run ()
    |> Async.RunSynchronously
    |> Result.defaultWith (fun error -> failwithf $"Failed to start EA.Telegram: %s{error.Message}")

    0
