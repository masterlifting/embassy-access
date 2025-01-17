[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Embassies.Russian
open EA.Core.DataAccess
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies
open EA.Telegram.Services.Consumer.Embassies.RussianEmbassy
open EA.Worker.Dependencies

module Kdmid =
    open Infrastructure.Logging
    open EA.Embassies.Russian.Kdmid.Dependencies

    type Dependencies =
        { RequestStorage: Request.RequestStorage
          pickOrder: Request list -> Async<Result<Request, Error' list>> }

        static member create ct (persistenceDeps: Persistence.Dependencies) (webDeps: Web.Dependencies) =
            let result = ResultBuilder()

            result {
                let! requestStorage = persistenceDeps.initRequestStorage ()

                let getRequestChats (request: Request) =
                    persistenceDeps.initTelegramChatStorage ()
                    |> ResultAsync.wrap (Chat.Query.findManyBySubscription request.Id)

                let notify notification =

                    let inline sendNotifications data =
                        webDeps.initTelegramClient ()
                        |> ResultAsync.wrap (Web.Telegram.Producer.produceSeq data ct)
                        |> ResultAsync.map ignore

                    match notification with
                    | Successfully(request, msg) ->
                        request
                        |> getRequestChats
                        |> ResultAsync.map (
                            Seq.map (fun chat -> chat.Id |> Kdmid.toSuccessfullyResponse (request, msg))
                        )
                        |> ResultAsync.bindAsync sendNotifications
                    | Unsuccessfully(request, error) ->
                        request
                        |> getRequestChats
                        |> ResultAsync.map (
                            Seq.map (fun chat -> chat.Id |> Kdmid.toUnsuccessfullyResponse (request, error))
                        )
                        |> ResultAsync.bindAsync sendNotifications
                    | HasAppointments(request, appointments) ->
                        request
                        |> getRequestChats
                        |> ResultAsync.bind (
                            Seq.map (fun chat -> chat.Id |> Kdmid.toHasAppointmentsResponse (request, appointments))
                            >> Result.choose
                        )
                        |> ResultAsync.bindAsync sendNotifications
                    | HasConfirmations(request, confirmations) ->
                        request
                        |> getRequestChats
                        |> ResultAsync.bind (
                            Seq.map (fun chat -> chat.Id |> Kdmid.toHasConfirmationsResponse (request, confirmations))
                            >> Result.choose
                        )
                        |> ResultAsync.bindAsync sendNotifications
                    |> ResultAsync.mapError (_.Message >> Log.critical)
                    |> Async.Ignore

                let pickOrder requests =
                    let deps = Order.Dependencies.create requestStorage ct
                    deps |> API.Order.Kdmid.pick requests notify

                return
                    { RequestStorage = requestStorage
                      pickOrder = pickOrder }
            }
