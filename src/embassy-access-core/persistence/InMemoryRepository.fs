﻿[<RequireQualifiedAccess>]
module internal EmbassyAccess.Persistence.InMemoryRepository

open Infrastructure
open Persistence.Domain
open Persistence.InMemory
open EmbassyAccess
open EmbassyAccess.Domain

[<Literal>]
let private RequestsKey = "requests"

module Query =
    module Request =
        open EmbassyAccess.Persistence.Query.Filter.Request
        open EmbassyAccess.Persistence.Query.Request

        module private Filters =
            let searchAppointments (filter: SearchAppointments) (request: Request) =
                filter.Embassy = request.Embassy
                && filter.HasStates request.ProcessState
                && filter.HasConfirmationState request.ConfirmationState

            let makeAppointment (filter: MakeAppointment) (request: Request) =
                filter.Embassy = request.Embassy
                && filter.HasStates request.ProcessState
                && filter.HasConfirmationStates request.ConfirmationState

        let getOne ct query storage =
            async {
                return
                    match ct |> notCanceled with
                    | true ->
                        let filter (data: Request list) =
                            match query with
                            | Id id -> data |> List.tryFind (fun x -> x.Id = id)
                            | First -> data |> List.tryHead
                            | Single ->
                                match data.Length with
                                | 1 -> Some data[0]
                                | _ -> None

                        storage
                        |> Query.Json.get RequestsKey
                        |> Result.bind (Seq.map Mapper.Request.toInternal >> Result.choose)
                        |> Result.map filter
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }

        let getMany ct query storage =
            async {
                return
                    match ct |> notCanceled with
                    | true ->
                        let filter (data: Request list) =
                            match query with
                            | SearchAppointments embassy ->
                                let query = SearchAppointments.create embassy

                                data
                                |> List.filter (Filters.searchAppointments query)
                                |> Query.paginate query.Pagination
                            | MakeAppointments embassy ->
                                let query = MakeAppointment.create embassy

                                data
                                |> List.filter (Filters.makeAppointment query)
                                |> Query.paginate query.Pagination

                        storage
                        |> Query.Json.get RequestsKey
                        |> Result.bind (Seq.map Mapper.Request.toInternal >> Result.choose)
                        |> Result.map filter
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }

module Command =
    module Request =
        open EmbassyAccess.Persistence.Command.Request
        open EmbassyAccess.Persistence.Command.Options.Request

        let private create create (requests: External.Request array) =
            match create with
            | PassportsGroup passportsGroup ->
                let embassy = passportsGroup.Embassy |> Mapper.Embassy.toExternal

                match
                    requests
                    |> Seq.exists (fun x -> x.Embassy = embassy && x.Payload = passportsGroup.Payload)
                with
                | true ->
                    Error
                    <| Operation
                        { Message =
                            $"Request for {passportsGroup.Embassy} with {passportsGroup.Payload} already exists."
                          Code = Some ErrorCodes.AlreadyExists }
                | _ ->
                    let request = passportsGroup.createRequest ()

                    match passportsGroup.Validation with
                    | Some validate -> request |> validate
                    | _ -> Ok()
                    |> Result.map (fun _ ->
                        let data = requests |> Array.append [| Mapper.Request.toExternal request |]

                        (data, request))

        let private update update (requests: External.Request array) =
            match update with
            | Request request ->
                match requests |> Array.tryFindIndex (fun x -> x.Id = request.Id.Value) with
                | None ->
                    Error
                    <| Operation
                        { Message = $"{request.Id} not found to update."
                          Code = Some ErrorCodes.NotFound }
                | Some index ->
                    let data =
                        requests
                        |> Array.mapi (fun i x -> if i = index then Mapper.Request.toExternal request else x)

                    Ok(data, request)

        let private delete delete (requests: External.Request array) =
            match delete with
            | RequestId requestId ->
                match requests |> Array.tryFindIndex (fun x -> x.Id = requestId.Value) with
                | None ->
                    Error
                    <| Operation
                        { Message = $"{requestId} not found to delete."
                          Code = Some ErrorCodes.NotFound }
                | Some index ->
                    requests[index]
                    |> Mapper.Request.toInternal
                    |> Result.map (fun request ->
                        let data = requests |> Array.removeAt index
                        (data, request))

        let execute ct operation storage =
            async {
                return
                    match ct |> notCanceled with
                    | true ->

                        storage
                        |> Query.Json.get RequestsKey
                        |> Result.bind (fun data ->
                            match operation with
                            | Create options -> data |> create options |> Result.map id
                            | Update options -> data |> update options |> Result.map id
                            | Delete options -> data |> delete options |> Result.map id)
                        |> Result.bind (fun (data, item) ->
                            storage |> Command.Json.save RequestsKey data |> Result.map (fun _ -> item))
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }
