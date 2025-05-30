﻿[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.AIProvider

open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Clients.Domain.OpenAI
open EA.Worker.Domain.Constants

type Dependencies = {
    initProvider: unit -> Result<AIProvider.Client.Provider, Error'>
} with

    static member create() =
        let result = ResultBuilder()

        let getEnv name =
            Configuration.Client.tryGetEnv name None
            |> Result.bind (function
                | Some value -> Ok value
                | None -> $"Environment configuration '{name}' not found." |> NotFound |> Error)

        result {

            let! openAiApiKey = getEnv OPENAI_API_KEY
            let! openAiProjectId = getEnv OPENAI_PROJECT_ID

            let initProvider () =
                {
                    Token = openAiApiKey
                    ProjectId = openAiProjectId
                }
                |> AIProvider.Client.Connection.OpenAI
                |> AIProvider.Client.init

            return { initProvider = initProvider }
        }
