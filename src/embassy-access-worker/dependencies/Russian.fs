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
    open EA.Embassies.Russian.Kdmid.Dependencies
    open EA.Embassies.Russian.Kdmid.Domain

    type Dependencies =
        { RequestStorage: Request.RequestStorage
          pickOrder: Request list -> Async<Result<Request, Error' list>> }

        static member create (schedule: Schedule) ct (persistenceDeps: Persistence.Dependencies) =
            let result = ResultBuilder()

            result {
                let! requestStorage = persistenceDeps.initRequestStorage ()

                let notificationDeps: Producer.Core.Dependencies =
                    { initChatStorage = persistenceDeps.initChatStorage
                      initRequestStorage = fun () -> requestStorage |> Ok }

                let notify notification =
                    notificationDeps
                    |> Producer.Produce.notification notification ct
                    |> Async.map ignore

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
