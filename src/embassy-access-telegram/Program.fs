open System.Threading
open Infrastructure
open Infrastructure.Prelude
open Infrastructure.Configuration.Domain
open Web.Clients
open AIProvider.Clients
open EA.Telegram.Dependencies

let private resultAsync = ResultAsyncBuilder()

[<EntryPoint>]
let main _ =

    Logging.Client.getLevel () |> Logging.Client.Console |> Logging.Client.init

    resultAsync {
        let! configuration =
            {
                Files = [ "appsettings.yml"; "embassies.yml"; "services.yml" ]
            }
            |> Configuration.Client.Yaml
            |> Configuration.Client.init
            |> async.Return

        let version =
            configuration
            |> Configuration.Client.getSection<string> "Version"
            |> Option.defaultValue "unknown"

        Logging.Log.inf $"EA.Telegram version: %s{version}"

        let ct = CancellationToken.None

        do! EA.Telegram.Initializer.run ()

        let! telegramClient =
            Telegram.Client.init {
                Token = Configuration.ENVIRONMENTS.TelegramBotToken
            }
            |> async.Return

        let! openApiClient =
            OpenAI.Client.init {
                Token = Configuration.ENVIRONMENTS.OpenAIApiKey
                ProjectId = "proj_OsfEwmtR7Shm2Uj4wqJTdgcC"
            }
            |> async.Return

        let! persistenceDeps = Persistence.Dependencies.create configuration |> async.Return

        let webDeps = telegramClient |> Web.Dependencies.create ct

        let! cultureDeps =
            persistenceDeps.initCultureStorage ()
            |> Result.map (fun storage ->
                Culture.Dependencies.create ct {
                    Provider = AIProvider.Client.OpenAI openApiClient
                    Storage = storage
                })
            |> async.Return

        return
            EA.Telegram.Client.start {
                ct = ct
                Web = webDeps
                Culture = cultureDeps
                Persistence = persistenceDeps
            }
            |> Async.map (fun _ -> 0 |> Ok)
    }
    |> Async.RunSynchronously
    |> Result.defaultWith (fun error -> failwithf $"Failed to start EA.Telegram: %s{error.Message}")
