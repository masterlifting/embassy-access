module EA.Embassies.Russian.Domain

open EA.Embassies.Russian
open Infrastructure

module Constants =
    [<Literal>]
    let internal EMBASSY_NAME = "Посольство РФ"

type ServiceInfo =
    { Id: Graph.NodeId
      Name: string
      Description: string option }

    interface Graph.INodeName with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.setName name = { this with Name = name }

module Midpass =
    type CheckReadiness =
        { Request: Midpass.Domain.Request }

        static member INFO =
            { Id = "34d311e0-ab72-411d-bb63-1d45fc76facc" |> Graph.NodeId.create
              Name = "Проверка готовности паспорта"
              Description =
                Some
                    @"Что бы воспользоваться услугой, пожалуйста,
                  добавьте к указанной комманде номер справки" }

        member this.Info = CheckReadiness.INFO

module Kdmid =
    [<Literal>]
    let private INSTRUCTION =
        @"Что бы воспользоваться услугой, пожалуйста,
            добавьте к указанной комманде ссылку, которую вы получили в email"

    type IssueForeign =
        { Request: Kdmid.Domain.ServiceRequest
          Dependencies: Kdmid.Domain.Dependencies }

        static member INFO =
            { Id = "687eabe4-3e64-45c6-a208-980f0a1738ba" |> Graph.NodeId.create
              Name = "Выпуск заграничного паспорта"
              Description = Some INSTRUCTION }

        member this.Info = IssueForeign.INFO

    type PowerOfAttorney =
        { Request: Kdmid.Domain.ServiceRequest
          Dependencies: Kdmid.Domain.Dependencies }

        static member INFO =
            { Id = "f37ad7b7-c872-49f7-9ae4-8789de657f81" |> Graph.NodeId.create
              Name = "Доверенность"
              Description = Some INSTRUCTION }

        member this.Info = PowerOfAttorney.INFO

    type CitizenshipRenunciation =
        { Request: Kdmid.Domain.ServiceRequest
          Dependencies: Kdmid.Domain.Dependencies }

        static member INFO =
            { Id = "7c74062a-10a4-4de2-8c51-8b72e1740932" |> Graph.NodeId.create
              Name = "Отказ от гражданства"
              Description = Some INSTRUCTION }

        member this.Info = CitizenshipRenunciation.INFO

type PassportService =
    | IssueForeign of Kdmid.IssueForeign
    | CheckReadiness of Midpass.CheckReadiness

    member this.Info =
        match this with
        | IssueForeign service -> service.Info
        | CheckReadiness service -> service.Info

    static member GRAPH =
        Graph.Node(
            { Id = "114067d4-5fdc-4815-8306-01a88367ff46" |> Graph.NodeId.create
              Name = "Пасспорт"
              Description = None },
            [ Graph.Node(Kdmid.IssueForeign.INFO, [])
              Graph.Node(Midpass.CheckReadiness.INFO, []) ]
        )

type NotaryService =
    | PowerOfAttorney of Kdmid.PowerOfAttorney

    member this.Info =
        match this with
        | PowerOfAttorney service -> service.Info

    static member GRAPH =
        Graph.Node(
            { Id = "9d904077-743e-4742-a490-f26aa68ff610" |> Graph.NodeId.create
              Name = "Нотариат"
              Description = None },
            [ Graph.Node(Kdmid.PowerOfAttorney.INFO, []) ]
        )

type CitizenshipService =
    | CitizenshipRenunciation of Kdmid.CitizenshipRenunciation

    member this.Info =
        match this with
        | CitizenshipRenunciation service -> service.Info

    static member GRAPH =
        Graph.Node(
            { Id = "5feaa2bd-132e-4a1e-8b24-e6f1833f9ebd" |> Graph.NodeId.create
              Name = "Гражданство"
              Description = None },
            [ Graph.Node(Kdmid.CitizenshipRenunciation.INFO, []) ]
        )

type Service =
    | Passport of PassportService
    | Notary of NotaryService
    | Citizenship of CitizenshipService

    member this.Info =
        match this with
        | Passport service -> service.Info
        | Notary service -> service.Info
        | Citizenship service -> service.Info

    static member GRAPH =
        Graph.Node(
            { Id = "60b6b7c3-ab36-400b-b0c7-5c3df98d50f3" |> Graph.NodeId.create
              Name = Constants.EMBASSY_NAME
              Description = None },
            [ PassportService.GRAPH; NotaryService.GRAPH; CitizenshipService.GRAPH ]
        )
