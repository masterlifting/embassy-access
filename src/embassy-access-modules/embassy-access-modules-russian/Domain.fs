module EA.Embassies.Russian.Domain

open EA.Embassies.Russian
open Infrastructure

module Constants =
    [<Literal>]
    let internal EMBASSY_NAME = "Посольство РФ"

type ServiceInfo =
    { Name: string
      Description: string option }

    interface Graph.INodeName with
        member this.Name = this.Name

module Midpass =
    type CheckReadiness =
        { Request: Midpass.Domain.Request }

        static member INFO =
            { Name = "Проверка готовности паспорта"
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
            { Name = "Выпуск заграничного паспорта"
              Description = Some INSTRUCTION }

        member this.Info = IssueForeign.INFO

    type PowerOfAttorney =
        { Request: Kdmid.Domain.ServiceRequest
          Dependencies: Kdmid.Domain.Dependencies }

        static member INFO =
            { Name = "Доверенность"
              Description = Some INSTRUCTION }

        member this.Info = PowerOfAttorney.INFO

    type CitizenshipRenunciation =
        { Request: Kdmid.Domain.ServiceRequest
          Dependencies: Kdmid.Domain.Dependencies }

        static member INFO =
            { Name = "Отказ от гражданства"
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
            { Name = "Пасспорт"
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
            { Name = "Нотариат"
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
            { Name = "Гражданство"
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
            { Name = Constants.EMBASSY_NAME
              Description = None },
            [ PassportService.GRAPH; NotaryService.GRAPH; CitizenshipService.GRAPH ]
        )
