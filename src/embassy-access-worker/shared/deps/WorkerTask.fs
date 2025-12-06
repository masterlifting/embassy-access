[<RequireQualifiedAccess>]
module EA.Worker.Dependencies.WorkerTask

open Microsoft.Extensions.Configuration
open Infrastructure.Prelude

let private result = ResultBuilder()

type Dependencies = {
    Configuration: IConfigurationRoot
    Persistence: Persistence.Dependencies
} with

    static member create configuration =
        result {
            let! persistence = Persistence.Dependencies.create ()

            return {
                Configuration = configuration
                Persistence = persistence
            }
        }
