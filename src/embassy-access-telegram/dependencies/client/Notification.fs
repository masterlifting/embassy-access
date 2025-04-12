[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Notification

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Domain

type Dependencies = {
    printPayload: string -> Result<string, Error'>
    getRequestChats: Request -> Async<Result<Chat list, Error'>>
    setRequestAppointments: Graph.NodeId -> Appointment Set -> Async<Result<Request list, Error'>>
    translateMessages: Culture -> Message seq -> Async<Result<Message list, Error'>>
    sendMessages: Message seq -> Async<Result<unit, Error'>>
}
