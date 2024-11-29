module EA.Embassies.Russian.Domain

open EA.Embassies.Russian
open Infrastructure

module Constants =
    [<Literal>]
    let internal EMBASSY_NAME = "Посольство РФ"

type ServiceInfo =
    { Id: Graph.NodeId
      Name: string
      Instruction: string option }

    interface Graph.INodeName with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.setName name = { this with Name = name }

module Midpass =
    type CheckReadiness =
        { Request: Midpass.Domain.Request }

        static member INFO =
            { Id = "34d311e0-ab72-411d-bb63-1d45fc76facc" |> Graph.NodeIdValue
              Name = "Проверка готовности паспорта"
              Instruction =
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
            { Id = "1.1.1" |> Graph.NodeIdValue
              Name = "Выпуск заграничного паспорта"
              Instruction = Some INSTRUCTION }

        member this.Info = IssueForeign.INFO

    type PowerOfAttorney =
        { Request: Kdmid.Domain.ServiceRequest
          Dependencies: Kdmid.Domain.Dependencies }

        static member INFO =
            { Id = "1.1.2" |> Graph.NodeIdValue
              Name = "Доверенность"
              Instruction = Some INSTRUCTION }

        member this.Info = PowerOfAttorney.INFO

    type CitizenshipRenunciation =
        { Request: Kdmid.Domain.ServiceRequest
          Dependencies: Kdmid.Domain.Dependencies }

        static member INFO =
            { Id = "7c74062a-10a4-4de2-8c51-8b72e1740932" |> Graph.NodeIdValue
              Name = "Отказ от гражданства"
              Instruction = Some INSTRUCTION }

        member this.Info = CitizenshipRenunciation.INFO

type PassportService =
    | IssueForeign of Kdmid.IssueForeign
    | CheckReadiness of Midpass.CheckReadiness

    member this.Info =
        match this with
        | IssueForeign service -> service.Info
        | CheckReadiness service -> service.Info

    static member internal GRAPH =
        Graph.Node(
            { Id = "1.1" |> Graph.NodeIdValue
              Name = "Пасспорт"
              Instruction = None },
            [ Graph.Node(Kdmid.IssueForeign.INFO, [])
              Graph.Node(Midpass.CheckReadiness.INFO, []) ]
        )

type NotaryService =
    | PowerOfAttorney of Kdmid.PowerOfAttorney

    member this.Info =
        match this with
        | PowerOfAttorney service -> service.Info

    static member internal GRAPH =
        Graph.Node(
            { Id = "1.2" |> Graph.NodeIdValue
              Name = "Нотариат"
              Instruction = None },
            [ Graph.Node(Kdmid.PowerOfAttorney.INFO, []) ]
        )

type CitizenshipService =
    | CitizenshipRenunciation of Kdmid.CitizenshipRenunciation

    member this.Info =
        match this with
        | CitizenshipRenunciation service -> service.Info

    static member internal GRAPH =
        Graph.Node(
            { Id = "1.3" |> Graph.NodeIdValue
              Name = "Гражданство"
              Instruction = None },
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
            { Id = "1" |> Graph.NodeIdValue
              Name = Constants.EMBASSY_NAME
              Instruction = None },
            [ PassportService.GRAPH; NotaryService.GRAPH; CitizenshipService.GRAPH ]
        )

module External =
    open System
    type ServiceInfo() =
        member val Id: string = String.Empty with get, set
        member val Name: string = String.Empty with get, set
        member val Instruction: string option = None with get, set
        member val Children: ServiceInfo[] = [||] with get, set