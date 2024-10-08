﻿module internal EmbassyAccess.Worker.Temporary

open Infrastructure
open Persistence
open Persistence.Domain
open Worker.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Persistence

let private createRussianSearchRequest ct (payload, country) =
    InMemory
    |> Storage.create
    |> ResultAsync.wrap (fun storage ->

        let options: Command.Options.Request.PassportsGroup =
            { Embassy = Russian country
              Payload = payload
              ConfirmationState = Disabled
              Validation = Some EmbassyAccess.Api.validateRequest }

        let operation =
            options
            |> Command.Options.Request.PassportsGroup
            |> Command.Request.Create

        storage
        |> Repository.Command.Request.execute ct operation
        |> ResultAsync.map (fun _ -> Success "Test request was created."))

let private createRussianConfirmRequest ct (payload, country) =
    Storage.create InMemory
    |> ResultAsync.wrap (fun storage ->

        let options: Command.Options.Request.PassportsGroup =
            { Embassy = Russian country
              Payload = payload
              ConfirmationState = Auto <| FirstAvailable
              Validation = Some EmbassyAccess.Api.validateRequest }

        let operation =
            options
            |> Command.Options.Request.PassportsGroup
            |> Command.Request.Create

        storage
        |> Repository.Command.Request.execute ct operation
        |> ResultAsync.map (fun _ -> Success "Test request was created."))

let createTestData ct =
    async {
        let! requestsToNotify =
            [ ("https://berlin.kdmid.ru/queue/OrderInfo.aspx?id=298905&cd=9D217A77", Germany Berlin)
              ("https://berlin.kdmid.ru/queue/OrderInfo.aspx?id=298907&cd=E66119D3", Germany Berlin)
              ("https://belgrad.kdmid.ru/queue/orderinfo.aspx?id=72096&cd=7FE4D97C&ems=7EE040C9", Serbia Belgrade)
              ("https://belgrad.kdmid.ru/queue/orderinfo.aspx?id=72095&cd=68672FA8&ems=742D4A80", Serbia Belgrade)
              ("https://belgrad.kdmid.ru/queue/OrderInfo.aspx?id=91373&cd=7EE89A15", Serbia Belgrade)
              ("https://sarajevo.kdmid.ru/queue/orderinfo.aspx?id=20779&cd=99CEBA38", Bosnia Sarajevo)
              ("https://sarajevo.kdmid.ru/queue/orderinfo.aspx?id=20780&cd=4FC17A57", Bosnia Sarajevo)
              ("https://sarajevo.kdmid.ru/queue/orderinfo.aspx?id=20781&cd=F23CB539", Bosnia Sarajevo)
              ("https://hague.kdmid.ru/queue/orderinfo.aspx?id=114878&cd=f1e14d11&ems=2CAA46D6", Netherlands Hague) ]
            |> List.map (createRussianSearchRequest ct)
            |> Async.Sequential

        let! requestsToAutoConfirm = [] |> List.map (createRussianConfirmRequest ct) |> Async.Sequential

        return requestsToNotify |> Seq.append requestsToAutoConfirm |> Result.choose
    }
