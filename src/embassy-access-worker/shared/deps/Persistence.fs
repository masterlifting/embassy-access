[<RequireQualifiedAccess>]
module EA.Worker.Dependencies.Persistence

open Infrastructure.Prelude
open EA.Worker.Dependencies

let private result = ResultBuilder()

type Dependencies = {
    ConnectionString: string
    EncryptionKey: string
} with

    static member create() =
        result {
            let connectionString = Configuration.ENVIRONMENTS.PostgresConnection
            let encryptionKey = Configuration.ENVIRONMENTS.EncryptionKey

            return {
                ConnectionString = connectionString
                EncryptionKey = encryptionKey
            }
        }
