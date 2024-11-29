[<RequireQualifiedAccess>]
module EA.Embassies.Russian.API

open Infrastructure
open EA.Embassies.Russian.Domain

module Service =
    let get service =
        match service with
        | Passport passportService ->
            match passportService with
            | IssueForeign issueForeign ->
                [ Constants.EMBASSY_NAME; passportService.Info.Name; issueForeign.Info.Name ]
                |> Graph.buildNodeNameOfSeq
                |> issueForeign.Request.Create
                |> Kdmid.Domain.StartOrder.create issueForeign.Request.TimeZone
                |> Kdmid.Order.start issueForeign.Dependencies
            | CheckReadiness checkReadiness ->
                [ Constants.EMBASSY_NAME; passportService.Info.Name; checkReadiness.Info.Name ]
                |> Graph.buildNodeNameOfSeq
                |> NotSupported
                |> Error
                |> async.Return
        | Notary notaryService ->
            match notaryService with
            | PowerOfAttorney powerOfAttorney ->
                [ Constants.EMBASSY_NAME; notaryService.Info.Name; powerOfAttorney.Info.Name ]
                |> Graph.buildNodeNameOfSeq
                |> powerOfAttorney.Request.Create
                |> Kdmid.Domain.StartOrder.create powerOfAttorney.Request.TimeZone
                |> Kdmid.Order.start powerOfAttorney.Dependencies
        | Citizenship citizenshipService ->
            match citizenshipService with
            | CitizenshipRenunciation citizenshipRenunciation ->
                [ Constants.EMBASSY_NAME; citizenshipService.Info.Name; service.Info.Name ]
                |> Graph.buildNodeNameOfSeq
                |> citizenshipRenunciation.Request.Create
                |> Kdmid.Domain.StartOrder.create citizenshipRenunciation.Request.TimeZone
                |> Kdmid.Order.start citizenshipRenunciation.Dependencies
                
    // let get' cfg serviceId request=
    //     
    //     let inline createService (request: EA.Core.Domain.Request) (node: Graph.Node<ServiceInfo>) =
    //         match node.FullId.Value with
    //         | "PASS-CHK" -> node.FullName |> NotSupported|> Error |> async.Return
    //         | _ -> request |> Kdmid.Domain.StartOrder.create node.TimeZone |> Kdmid.Order.start cfg
    //             
    //     
    //     cfg
    //     |> Settings.ServiceInfo.getGraph
    //     |> ResultAsync.bindAsync (fun graph ->
    //         graph
    //         |> Graph.BFS.tryFindById serviceId
    //         |> Option.map Ok
    //         |> Option.defaultValue ($"ServiceId {serviceId.Value}" |> NotFound |> Error)
    //         |> ResultAsync.wrap createService)
        

module Order =
    module Kdmid =
        let start deps order = order |> Kdmid.Order.start deps
        let pick deps order = order |> Kdmid.Order.pick deps
