[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Service

open EA.Core.Domain
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.Dependencies.Embassies

type Dependencies = {
    processRequest: Request -> Async<Result<Request, Error'>>
    printPayload: string -> Result<string, Error'>
} with

    static member create(serviceId: Graph.NodeId) =
        fun (deps: Embassies.Dependencies) ->
            let requestIdParts = serviceId.Split() |> List.skip 1

            match requestIdParts |> List.tryHead with
            | Some Embassies.RUS ->
                match requestIdParts |> List.skip 2 with
                | [ "0"; "0"; "1" ] ->
                    {
                        printPayload =
                            EA.Russian.Services.Domain.Midpass.Payload.create
                            >> Result.map EA.Russian.Services.Domain.Midpass.Payload.print
                        processRequest =
                            fun request ->
                                deps.Russian
                                |> EA.Telegram.Dependencies.Embassies.Russian.Midpass.Dependencies.create
                                |> fun x -> x.Service |> EA.Russian.Services.Midpass.Client.init
                                |> ResultAsync.wrap (EA.Russian.Services.Midpass.Service.tryProcess request)
                    }
                    |> Ok
                | _ ->
                    {
                        printPayload =
                            EA.Russian.Services.Domain.Kdmid.Payload.create
                            >> Result.map EA.Russian.Services.Domain.Kdmid.Payload.print
                        processRequest =
                            fun request ->
                                deps.Russian
                                |> EA.Telegram.Dependencies.Embassies.Russian.Kdmid.Dependencies.create
                                |> Result.bind (fun x -> x.Service |> EA.Russian.Services.Kdmid.Client.init)
                                |> ResultAsync.wrap (EA.Russian.Services.Kdmid.Service.tryProcess request)
                    }
                    |> Ok
            | Some Embassies.ITA ->
                {
                    printPayload =
                        EA.Italian.Services.Domain.Prenotami.Payload.create
                        >> Result.map EA.Italian.Services.Domain.Prenotami.Payload.print
                    processRequest =
                        fun request ->
                            deps.Italian
                            |> EA.Telegram.Dependencies.Embassies.Italian.Prenotami.Dependencies.create
                            |> fun x -> x.Service |> EA.Italian.Services.Prenotami.Client.init
                            |> ResultAsync.wrap (EA.Italian.Services.Prenotami.Service.tryProcess request)
                }
                |> Ok
            | _ ->
                $"Service '%s{serviceId.Value}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
