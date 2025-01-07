[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open Worker.Domain
open EA.Core.Domain
open EA.Embassies.Russian
open EA.Core.DataAccess
open EA.Telegram
open EA.Telegram.Dependencies
open EA.Worker.Dependencies

module Kdmid =
    open Infrastructure.Logging
    open EA.Embassies.Russian.Kdmid.Dependencies
    open EA.Embassies.Russian.Kdmid.Domain

    type Dependencies =
        { RequestStorage: Request.RequestStorage
          pickOrder: Request list -> Async<Result<Request, Error' list>> }

        static member create (schedule: Schedule) ct (deps: Persistence.Dependencies) =
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
                    let timeZone = schedule.TimeZone |> float

                    let startOrders =
                        requests
                        |> List.map (fun request ->
                            { Request = request
                              TimeZone = timeZone })

                    let order =
                        { StartOrders = startOrders
                          notify = notify }

                    deps |> API.Order.Kdmid.pick order

                return
                    { RequestStorage = requestStorage
                      pickOrder = pickOrder }
            }
