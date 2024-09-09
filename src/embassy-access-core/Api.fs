[<RequireQualifiedAccess>]
module EmbassyAccess.Api

open EmbassyAccess.Embassies

type ProcessRequestDeps = Russian of Russian.Domain.ProcessRequestDeps
type SendMessageDeps = Russian of Russian.Domain.SendMessageDeps
type ReceiveMessageDeps = Russian of Russian.Domain.ReceiveMessageDeps

let processRequest deps request =
    match deps with
    | ProcessRequestDeps.Russian deps -> request |> Russian.Core.processRequest deps

let sendMessage deps notification =
    match deps with
    | SendMessageDeps.Russian deps -> notification |> Russian.Message.send deps

let receiveMessage deps listener =
    match deps with
    | ReceiveMessageDeps.Russian deps -> listener |> Russian.Message.receive deps
