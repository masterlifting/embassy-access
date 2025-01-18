module internal EA.Worker.Dependencies.Embassies.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Embassies.Russian
open EA.Core.DataAccess
open EA.Telegram.Dependencies
open EA.Worker.Dependencies
open EA.Telegram.Dependencies.Producer.Embassies.RussianEmbassy
open EA.Telegram.Services.Producer.Embassies.Russian.Service

module Kdmid =
    open Infrastructure.Logging
    open EA.Telegram.Dependencies.Producer
    open EA.Embassies.Russian.Kdmid.Dependencies

    type Dependencies =
        { RequestStorage: Request.RequestStorage
          pickOrder: Request list -> Async<Result<Request, Error' list>> }

        static member create ct (persistenceDeps: Persistence.Dependencies) (webDeps: Web.Dependencies) =
            let result = ResultBuilder()

            result {
                let! requestStorage = persistenceDeps.initRequestStorage ()

                let telegramProducerDeps: Producer.Dependencies =
                    { CancellationToken = ct
                      initTelegramClient = webDeps.initTelegramClient
                      initChatStorage = persistenceDeps.initChatStorage }

                let! telegramProducerKdmidDeps = Kdmid.Dependencies.create telegramProducerDeps

                let notify notification =
                    telegramProducerKdmidDeps
                    |> Kdmid.sendNotification notification
                    |> ResultAsync.mapError (_.Message >> Log.critical)
                    |> Async.Ignore

                let pickOrder requests =
                    let deps = Order.Dependencies.create requestStorage ct
                    deps |> API.Order.Kdmid.pick requests notify

                return
                    { RequestStorage = requestStorage
                      pickOrder = pickOrder }
            }
