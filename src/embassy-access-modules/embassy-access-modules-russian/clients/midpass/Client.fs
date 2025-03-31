module EA.Russian.Clients.Midpass.Client

open Infrastructure.Domain
open EA.Russian.Clients.Domain

let private clients = Midpass.ClientFactory()

let init (_: Midpass.Dependencies) =
    "Midpass client is not implemented." + EA.Core.Domain.Constants.NOT_IMPLEMENTED
    |> NotImplemented
    |> Error
