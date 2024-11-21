module EA.Embassies.Russian.Domain

open EA.Embassies.Russian
open Infrastructure

type ServiceInfo =
    { Name: string
      Instruction: string option }

    interface Graph.INodeName with
        member this.Name = this.Name


module Midpass =
    type CheckReadiness =
        { Request: Midpass.Domain.Request }

        static member INFO =
            { Name = "Проверка готовности паспорта"
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
            { Name = "Выпуск заграничного паспорта"
              Instruction = Some INSTRUCTION }

        member this.Info = IssueForeign.INFO

    type PowerOfAttorney =
        { Request: Midpass.Domain.Request }

        static member INFO =
            { Name = "Доверенность"
              Instruction = Some INSTRUCTION }

        member this.Info = PowerOfAttorney.INFO

    type CitizenshipRenunciation =
        { Request: Kdmid.Domain.ServiceRequest
          Dependencies: Kdmid.Domain.Dependencies }

        static member INFO =
            { Name = "Отказ от гражданства"
              Instruction = Some INSTRUCTION }

        member this.Info = CitizenshipRenunciation.INFO

type PassportService =
    | IssueForeign of Kdmid.IssueForeign
    | CheckReadiness of Midpass.CheckReadiness

    member this.Info =
        match this with
        | IssueForeign service -> service.Info
        | CheckReadiness service -> service.Info

    static member LIST = [ Kdmid.IssueForeign.INFO; Midpass.CheckReadiness.INFO ]

    static member internal MAP =
        [ Kdmid.IssueForeign.INFO.Name, Map.empty<string, ServiceInfo>
          Midpass.CheckReadiness.INFO.Name, Map.empty<string, ServiceInfo> ]
        |> Map

    static member internal GRAPH = Graph.Node(Kdmid.IssueForeign.INFO, [])


type NotaryService =
    | PowerOfAttorney of Kdmid.PowerOfAttorney

    member this.Info =
        match this with
        | PowerOfAttorney service -> service.Info

    static member LIST = [ Kdmid.PowerOfAttorney.INFO ]

    static member internal MAP =
        [ Kdmid.PowerOfAttorney.INFO.Name, Map.empty<string, ServiceInfo> ] |> Map

type CitizenshipService =
    | CitizenshipRenunciation of Kdmid.CitizenshipRenunciation

    member this.Info =
        match this with
        | CitizenshipRenunciation service -> service.Info

    static member LIST = [ Kdmid.CitizenshipRenunciation.INFO ]

    static member internal MAP =
        [ Kdmid.CitizenshipRenunciation.INFO.Name, Map.empty<string, ServiceInfo> ]
        |> Map

type Service =
    | Passport of PassportService
    | Notary of NotaryService
    | Citizenship of CitizenshipService

    static member private PASSPORT_INFO = { Name = "Паспорт"; Instruction = None }

    static member private NOTARY_INFO =
        { Name = "Нотариат"
          Instruction = None }

    static member private CITIZENSHIP_INFO =
        { Name = "Гражданство"
          Instruction = None }

    member this.Info =
        match this with
        | Passport _ -> Service.PASSPORT_INFO
        | Notary _ -> Service.NOTARY_INFO
        | Citizenship _ -> Service.CITIZENSHIP_INFO

    static member LIST = [ Service.PASSPORT_INFO; Service.NOTARY_INFO; Service.CITIZENSHIP_INFO ]

    static member internal MAP =
        [ Service.PASSPORT_INFO.Name, PassportService.MAP
          Service.NOTARY_INFO.Name, NotaryService.MAP
          Service.CITIZENSHIP_INFO.Name, CitizenshipService.MAP ]
        |> Map

    static member internal GRAPH =
        Graph.Node(
            Service.PASSPORT_INFO, [ PassportService.GRAPH ]
            )

    static member getNext depth service =

        let rec innerLoop d items =
            if d = depth then
                items |> Map.tryFind service
            else
                items |> Map.tryFind service

        Service.MAP |> innerLoop 0

    member this.CreateRequest() =
        match this with
        | Passport service ->
            match service with
            | IssueForeign service -> service.Request.CreateRequest service.Info.Name
            | CheckReadiness service -> service.Request.CreateRequest service.Info.Name
        | Notary service ->
            match service with
            | PowerOfAttorney service -> service.Request.CreateRequest service.Info.Name
        | Citizenship service ->
            match service with
            | CitizenshipRenunciation service -> service.Request.CreateRequest service.Info.Name
