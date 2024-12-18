﻿[<RequireQualifiedAccess>]
module internal EA.Persistence.FileSystemRepository

open Infrastructure
open Persistence.Domain
open Persistence.FileSystem
open EA.Domain

module Query =
    module Request =
        open EA.Persistence.Query.Filter.Request
        open EA.Persistence.Query.Request

        let getOne query ct storage =
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
                |> Query.Json.get
                |> ResultAsync.bind (Seq.map EA.Mapper.Request.toInternal >> Result.choose)
                |> ResultAsync.map filter
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return

        let getMany query ct storage =
            match ct |> notCanceled with
            | true ->
                let filter (data: Request list) =
                    match query with
                    | SearchAppointments embassy ->
                        let query = SearchAppointments.create embassy

                        data
                        |> List.filter (InMemory.searchAppointments query)
                        |> Query.paginate query.Pagination
                    | MakeAppointments embassy ->
                        let query = MakeAppointment.create embassy

                        data
                        |> List.filter (InMemory.makeAppointment query)
                        |> Query.paginate query.Pagination
                    | ByIds requestIds -> data |> List.filter (fun x -> requestIds.Contains x.Id)
                    | ByEmbassy embassy -> data |> List.filter (fun x -> x.Embassy = embassy)

                storage
                |> Query.Json.get
                |> ResultAsync.bind (Seq.map EA.Mapper.Request.toInternal >> Result.choose)
                |> ResultAsync.map filter
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return

module Command =
    module Request =
        open EA.Persistence.Command.Request
        open EA.Persistence.Command.Definitions.Request

        let private create definition (requests: External.Request array) =
            match definition with
            | Create.PassportsGroup passportsGroup ->
                let embassy = passportsGroup.Embassy |> EA.Mapper.Embassy.toExternal

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
                        let data = requests |> Array.append [| EA.Mapper.Request.toExternal request |]

                        (data, request))

        let private createOrUpdate definition (requests: External.Request array) =
            match definition with
            | CreateOrUpdate.PassportsGroup passportsGroup ->
                let embassy = passportsGroup.Embassy |> EA.Mapper.Embassy.toExternal

                match
                    requests
                    |> Seq.tryFind (fun x -> x.Embassy = embassy && x.Payload = passportsGroup.Payload)
                with
                | Some request ->
                    let data =
                        requests |> Array.mapi (fun i x -> if x.Id = request.Id then request else x)

                    request
                    |> EA.Mapper.Request.toInternal
                    |> Result.map (fun request -> (data, request))
                | None ->
                    let request = passportsGroup.createRequest ()

                    match passportsGroup.Validation with
                    | Some validate -> request |> validate
                    | _ -> Ok()
                    |> Result.map (fun _ ->
                        let data = requests |> Array.append [| EA.Mapper.Request.toExternal request |]
                        (data, request))
            | CreateOrUpdate.OthersGroup othersGroup ->
                let embassy = othersGroup.Embassy |> EA.Mapper.Embassy.toExternal

                match
                    requests
                    |> Seq.tryFind (fun x -> x.Embassy = embassy && x.Payload = othersGroup.Payload)
                with
                | Some request ->
                    let data =
                        requests |> Array.mapi (fun i x -> if x.Id = request.Id then request else x)

                    request
                    |> EA.Mapper.Request.toInternal
                    |> Result.map (fun request -> (data, request))
                | None ->
                    let request = othersGroup.createRequest ()

                    match othersGroup.Validation with
                    | Some validate -> request |> validate
                    | _ -> Ok()
                    |> Result.map (fun _ ->
                        let data = requests |> Array.append [| EA.Mapper.Request.toExternal request |]
                        (data, request))
            | CreateOrUpdate.PassportResultGroup passportResultGroup ->
                let embassy = passportResultGroup.Embassy |> EA.Mapper.Embassy.toExternal

                match
                    requests
                    |> Seq.tryFind (fun x -> x.Embassy = embassy && x.Payload = passportResultGroup.Payload)
                with
                | Some request ->
                    let data =
                        requests |> Array.mapi (fun i x -> if x.Id = request.Id then request else x)

                    request
                    |> EA.Mapper.Request.toInternal
                    |> Result.map (fun request -> (data, request))
                | None ->
                    let request = passportResultGroup.createRequest ()

                    match passportResultGroup.Validation with
                    | Some validate -> request |> validate
                    | _ -> Ok()
                    |> Result.map (fun _ ->
                        let data = requests |> Array.append [| EA.Mapper.Request.toExternal request |]
                        (data, request))

        let private update definition (requests: External.Request array) =
            match definition with
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
                        |> Array.mapi (fun i x ->
                            if i = index then
                                EA.Mapper.Request.toExternal request
                            else
                                x)

                    Ok(data, request)

        let private delete definition (requests: External.Request array) =
            match definition with
            | RequestId requestId ->
                match requests |> Array.tryFindIndex (fun x -> x.Id = requestId.Value) with
                | None ->
                    Error
                    <| Operation
                        { Message = $"{requestId} not found to delete."
                          Code = Some ErrorCodes.NotFound }
                | Some index ->
                    requests[index]
                    |> EA.Mapper.Request.toInternal
                    |> Result.map (fun request ->
                        let data = requests |> Array.removeAt index
                        (data, request))

        let execute command ct storage =
            match ct |> notCanceled with
            | true ->

                storage
                |> Query.Json.get
                |> ResultAsync.bind (fun data ->
                    match command with
                    | Create definition -> data |> create definition |> Result.map id
                    | CreateOrUpdate definition -> data |> createOrUpdate definition |> Result.map id
                    | Update definition -> data |> update definition |> Result.map id
                    | Delete definition -> data |> delete definition |> Result.map id)
                |> ResultAsync.bindAsync (fun (data, item) ->
                    storage |> Command.Json.save data |> ResultAsync.map (fun _ -> item))
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return
