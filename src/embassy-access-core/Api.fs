[<RequireQualifiedAccess>]
module EmbassyAccess.Api

open Infrastructure
open EmbassyAccess.Embassies

type ProcessRequestDeps = Russian of Russian.Domain.ProcessRequestDeps
type SendMessageDeps = Russian of Async<Result<unit, Error'>>
type ReceiveMessagesDeps = Russian of Result<Web.Domain.Listener, Error'>

let processRequest deps request =
    match deps with
    | ProcessRequestDeps.Russian deps -> request |> Russian.Core.processRequest deps

let sendMessage deps =
    match deps with
    | SendMessageDeps.Russian send -> send

let receiveMessages ct deps =
    match deps with
    | ReceiveMessagesDeps.Russian listener -> listener
    |> Web.Client.listen ct
