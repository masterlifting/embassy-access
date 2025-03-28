﻿[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Persistence

open EA.Core.Domain
open EA.Telegram.Domain
open Infrastructure.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess
open Infrastructure.Prelude

type Dependencies = {
    ChatStorage: Chat.ChatStorage
    RequestStorage: Request.RequestStorage
    setRequestAppointments: Graph.NodeId -> Appointment Set -> Async<Result<Request list, Error'>>
    getRequestChats: Request -> Async<Result<Chat list, Error'>>
    getEmbassyGraph: unit -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
    getServiceGraph: unit -> Async<Result<Graph.Node<ServiceNode>, Error'>>
} with

    static member create ct =
        fun
            (initChatStorage: unit -> Result<Chat.ChatStorage, Error'>)
            (initRequestStorage: unit -> Result<Request.RequestStorage, Error'>)
            (initServiceGraphStorage: unit -> Result<ServiceGraph.ServiceGraphStorage, Error'>)
            (initEmbassyGraphStorage: unit -> Result<EmbassyGraph.EmbassyGraphStorage, Error'>) ->

            let result = ResultBuilder()

            result {

                let! chatStorage = initChatStorage ()
                let! requestStorage = initRequestStorage ()

                let getRequestChats (request: Request) =
                    requestStorage
                    |> Request.Query.findManyByServiceId request.Service.Id
                    |> ResultAsync.map (Seq.map _.Id)
                    |> ResultAsync.bindAsync (fun subscriptionIds ->
                        chatStorage |> Chat.Query.findManyBySubscriptions subscriptionIds)

                let getServiceGraph () =
                    initServiceGraphStorage () |> ResultAsync.wrap ServiceGraph.get

                let getEmbassyGraph () =
                    initEmbassyGraphStorage () |> ResultAsync.wrap EmbassyGraph.get

                let setRequestAppointments serviceId appointments =
                    requestStorage
                    |> Request.Query.findManyByServiceId serviceId
                    |> ResultAsync.map (fun requests ->
                        requests
                        |> Seq.map (fun request -> {
                            request with
                                Appointments = appointments
                        }))
                    |> ResultAsync.bindAsync (fun requests -> requestStorage |> Request.Command.updateSeq requests)

                return {
                    ChatStorage = chatStorage
                    RequestStorage = requestStorage
                    getRequestChats = getRequestChats
                    setRequestAppointments = setRequestAppointments
                    getEmbassyGraph = getEmbassyGraph
                    getServiceGraph = getServiceGraph
                }
            }
