[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.AIProvider

open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Domain
open EA.Worker.Domain.Constants

type Dependencies =
    { initProvider: unit -> Result<AIProvider.Client.Provider, Error'> }

    static member create() =
        let result = ResultBuilder()

        let inline getEnv name =
            Configuration.getEnvVar name
            |> Result.bind (function
                | Some value -> Ok value
                | None -> $"'{name}' in the configuration." |> NotFound |> Error)

        result {

            let! openAiApiKey = getEnv OPENAI_API_KEY
            let! openAiProjectId = getEnv OPENAI_PROJECT_ID

            let initProvider () =
                { Token = openAiApiKey
                  ProjectId = openAiProjectId }
                |> AIProvider.Client.Connection.OpenAI
                |> AIProvider.Client.init

            return { initProvider = initProvider }
        }
