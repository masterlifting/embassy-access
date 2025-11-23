[<RequireQualifiedAccess>]
module EA.Worker.Dependencies.WorkerTask

open Microsoft.Extensions.Configuration

type Dependencies = { Configuration: IConfigurationRoot }
