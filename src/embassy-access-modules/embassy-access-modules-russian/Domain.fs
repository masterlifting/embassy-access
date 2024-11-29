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
        member this.set(id, name) = { this with Id = id; Name = name }

module Midpass =
    type CheckReadiness =
        { Request: Midpass.Domain.Request }

        static member INFO =
            { Id = "CHK" |> Graph.NodeIdValue
              Name = "Проверка готовности паспорта"
              Instruction =
                Some
                    "Что бы воспользоваться услугой, добавьте номер справки, которую вы получили после подачи документов, после символа | в вышеуказанную команду и отправьте ее в чат." }

        member this.Info = CheckReadiness.INFO

module Kdmid =
    [<Literal>]
    let private INSTRUCTION =
        "Что бы воспользоваться услугой, добавьте ссылку, которую вы получили в email при регистрации после символа | в вышеуказанную команду и отправьте ее в чат."

    type IssueForeign =
        { Request: Kdmid.Domain.ServiceRequest
          Dependencies: Kdmid.Domain.Dependencies }

        static member INFO =
            { Id = "ISS" |> Graph.NodeIdValue
              Name = "Выпуск заграничного паспорта"
              Instruction = Some INSTRUCTION }

        member this.Info = IssueForeign.INFO

    type PowerOfAttorney =
        { Request: Kdmid.Domain.ServiceRequest
          Dependencies: Kdmid.Domain.Dependencies }

        static member INFO =
            { Id = "PWR" |> Graph.NodeIdValue
              Name = "Доверенность"
              Instruction = Some INSTRUCTION }

        member this.Info = PowerOfAttorney.INFO

    type CitizenshipRenunciation =
        { Request: Kdmid.Domain.ServiceRequest
          Dependencies: Kdmid.Domain.Dependencies }

        static member INFO =
            { Id = "REN" |> Graph.NodeIdValue
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
            { Id = "PASS" |> Graph.NodeIdValue
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
            { Id = "RPL" |> Graph.NodeIdValue
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
            { Id = "VSA" |> Graph.NodeIdValue
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
            { Id = "EMB.RU" |> Graph.NodeIdValue
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
