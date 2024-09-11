[<RequireQualifiedAccess>]
module EmbassyAccess.Api

open Infrastructure
open EmbassyAccess.Embassies

type ProcessRequestDeps = Russian of Russian.Domain.ProcessRequestDeps
type SendMessageDeps = Russian of Russian.Domain.SendMessageDeps
type ReceiveMessageDeps = Russian of Result<Russian.Domain.ReceiveMessageDeps, Error'>

let processRequest deps request =
    match deps with
    | ProcessRequestDeps.Russian deps -> request |> Russian.Core.processRequest deps

let sendMessage deps notification =
    match deps with
    | SendMessageDeps.Russian deps -> notification |> Russian.Message.send deps

let listener deps =
    match deps with
    | ReceiveMessageDeps.Russian deps -> deps |> Result.map _.Listener
