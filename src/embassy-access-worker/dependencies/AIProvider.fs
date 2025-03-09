[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.AIProvider

open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider
open EA.Worker.Domain.Constants

type Dependencies =
    { initProvider: unit -> Result<Client.Provider, Error'> }

    static member create() =
        let result = ResultBuilder()

        result {

            let initProvider () =
                Configuration.getEnvVar OPENAI_API_KEY
                |> Result.bind (function
                    | Some token ->
                        { AIProvider.OpenAI.Domain.Token = token }
                        |> Client.Connection.OpenAI
                        |> Client.init
                    | None -> $"'{OPENAI_API_KEY}' in the configuration." |> NotFound |> Error)

            return { initProvider = initProvider }
        }
