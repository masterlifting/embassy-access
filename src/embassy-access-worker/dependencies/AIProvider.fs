[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.AIProvider

open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider

type Dependencies =
    { initProvider: unit -> Result<Client.Provider, Error'> }

    static member create cfg =
        let result = ResultBuilder()

        result {

            let! openAiKey =
                cfg
                |> Configuration.getSection<string> "OpenAI:APIKey"
                |> Option.map Ok
                |> Option.defaultValue ("Section 'OpenAI:APIKey' in the configuration." |> NotFound |> Error)

            let initProvider () =
                { OpenAI.Domain.Connection.Token = openAiKey }
                |> Client.Connection.OpenAI
                |> Client.init

            return { initProvider = initProvider }
        }