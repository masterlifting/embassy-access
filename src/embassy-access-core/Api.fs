[<RequireQualifiedAccess>]
module EmbassyAccess.Api

open Infrastructure
open EmbassyAccess.Embassies

type ProcessRequestDeps = Russian of Russian.Domain.ProcessRequestDeps
type SendMessageDeps = Russian of Async<Result<int, Error'>>

let processRequest deps request =
    match deps with
    | ProcessRequestDeps.Russian deps -> request |> Russian.Core.processRequest deps

let sendMessage deps =
    match deps with
    | SendMessageDeps.Russian send -> send

let receiveMessages ct context =
    match context with
    | Web.Domain.Telegram token ->
        Web.Telegram.Client.create token
        |> Result.map (fun client -> Web.Domain.Listener.Telegram(client, Telegram.receive ct))
    | _ -> Error <| NotSupported $"Context '{context}'.. EmbassyAccess.Api.receiveMessages"
    |> Web.Client.listen ct
