[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Embassies.Russian
open EA.Core.DataAccess
open EA.Telegram
open EA.Telegram.Dependencies
open EA.Worker.Dependencies

module Kdmid =
    open Infrastructure.Logging
    open EA.Embassies.Russian.Kdmid.Dependencies

    type Dependencies =
        { RequestStorage: Request.RequestStorage
          pickOrder: Request list -> Async<Result<Request, Error' list>> }

        static member create ct (deps: Persistence.Dependencies) =
            let result = ResultBuilder()

            result {
                let! requestStorage = deps.initRequestStorage ()
                
                let! producerDeps =
                    Producer.Core.Dependencies.create
                        ct
                        { initChatStorage = deps.initTelegramChatStorage
                          initRequestStorage = fun () -> requestStorage |> Ok
                          initServiceGraphStorage = fun () -> "initServiceGraphStorage" |> NotImplemented |> Error
                          initEmbassyGraphStorage = fun () -> "initEmbassyGraphStorage" |> NotImplemented |> Error }

                let notify notification =
                    producerDeps
                    |> Producer.Produce.notification notification
                    |> ResultAsync.mapError (_.Message >> Log.critical)
                    |> Async.Ignore

                let pickOrder requests =
                    let deps = Order.Dependencies.create requestStorage ct
                    deps |> API.Order.Kdmid.pick requests notify

                return
                    { RequestStorage = requestStorage
                      pickOrder = pickOrder }
            }
