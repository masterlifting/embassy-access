[<RequireQualifiedAccess>]
module EmbassyAccess.Api

open Infrastructure
open EmbassyAccess.Embassies

type ProcessRequestDeps = Russian of Russian.Domain.ProcessRequestDeps
type SendMessageDeps = Russian of Russian.Domain.RequestSender
type ReceiveMessageDeps = Russian of Result<Web.Domain.Listener, Error'>

let processRequest deps request =
    match deps with
    | ProcessRequestDeps.Russian deps -> request |> Russian.Core.processRequest deps

let sendMessage deps message =
    match deps with
    | SendMessageDeps.Russian sender -> sender |> Russian.Telegram.send message

let listenMessages ct deps =
    match deps with
    | ReceiveMessageDeps.Russian listener -> listener
    |> ResultAsync.wrap (Web.Client.listen ct)
