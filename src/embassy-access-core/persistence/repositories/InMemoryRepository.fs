[<RequireQualifiedAccess>]
module internal EA.Persistence.InMemoryRepository

open Infrastructure
open Persistence.Domain
open Persistence.InMemory
open EA.Domain

module Query =
    module Request =
        open EA.Persistence.Query.Filter.Request
        open EA.Persistence.Query.Request

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
                        |> Query.Json.get Key.Requests
                        |> Result.bind (Seq.map EA.Mapper.Request.toInternal >> Result.choose)
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
                                |> List.filter (InMemory.searchAppointments query)
                                |> Query.paginate query.Pagination
                            | MakeAppointments embassy ->
                                let query = MakeAppointment.create embassy

                                data
                                |> List.filter (InMemory.makeAppointment query)
                                |> Query.paginate query.Pagination

                        storage
                        |> Query.Json.get Key.Requests
                        |> Result.bind (Seq.map EA.Mapper.Request.toInternal >> Result.choose)
                        |> Result.map filter
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }

module Command =
    module Request =
        open EA.Persistence.Command.Request
        open EA.Persistence.Command.Options.Request

        let private create create (requests: External.Request array) =
            match create with
            | PassportsGroup passportsGroup ->
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
                        |> Array.mapi (fun i x ->
                            if i = index then
                                EA.Mapper.Request.toExternal request
                            else
                                x)

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
                    |> EA.Mapper.Request.toInternal
                    |> Result.map (fun request ->
                        let data = requests |> Array.removeAt index
                        (data, request))

        let execute ct operation storage =
            async {
                return
                    match ct |> notCanceled with
                    | true ->

                        storage
                        |> Query.Json.get Key.Requests
                        |> Result.bind (fun data ->
                            match operation with
                            | Create options -> data |> create options |> Result.map id
                            | Update options -> data |> update options |> Result.map id
                            | Delete options -> data |> delete options |> Result.map id)
                        |> Result.bind (fun (data, item) ->
                            storage |> Command.Json.save Key.Requests data |> Result.map (fun _ -> item))
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }
