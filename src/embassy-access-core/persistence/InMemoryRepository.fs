[<RequireQualifiedAccess>]
module internal EmbassyAccess.Persistence.InMemoryRepository

open Infrastructure
open Persistence.Domain
open EmbassyAccess.Domain
open Persistence.InMemory

module Request =

    [<Literal>]
    let private Key = "requests"

    module Query =
        module private Filters =
            let searchAppointments (filter: Query.Request.Filter.SearchAppointments) (request: Request) =
                filter.Embassy = request.Embassy
                && filter.HasStates request.ProcessState
                && filter.HasConfirmationState request.ConfirmationState

            let makeAppointment (filter: Query.Request.Filter.MakeAppointment) (request: Request) =
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
                            | Query.Request.Id id -> data |> List.tryFind (fun x -> x.Id = id)
                            | Query.Request.First -> data |> List.tryHead
                            | Query.Request.Single ->
                                match data.Length with
                                | 1 -> Some data[0]
                                | _ -> None

                        storage
                        |> Query.Json.get Key
                        |> Result.bind (Seq.map EmbassyAccess.Mapper.Request.toInternal >> Result.choose)
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
                            | Query.Request.SearchAppointments embassy ->
                                let query = Query.Request.Filter.SearchAppointments.create embassy

                                data
                                |> List.filter (Filters.searchAppointments query)
                                |> Query.paginate query.Pagination
                            | Query.Request.MakeAppointments embassy ->
                                let query = Query.Request.Filter.MakeAppointment.create embassy

                                data
                                |> List.filter (Filters.makeAppointment query)
                                |> Query.paginate query.Pagination

                        storage
                        |> Query.Json.get Key
                        |> Result.bind (Seq.map EmbassyAccess.Mapper.Request.toInternal >> Result.choose)
                        |> Result.map filter
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }

    module Command =
        let private create create (requests: External.Request array) =
            match create with
            | Command.Request.Options.PassportsGroup passportsGroup ->
                let embassy = passportsGroup.Embassy |> EmbassyAccess.Mapper.Embassy.toExternal

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
                        let data =
                            requests |> Array.append [| EmbassyAccess.Mapper.Request.toExternal request |]

                        (data, request))

        let private update update (requests: External.Request array) =
            match update with
            | Command.Request.Options.Request request ->
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
                                EmbassyAccess.Mapper.Request.toExternal request
                            else
                                x)

                    Ok(data, request)

        let private delete delete (requests: External.Request array) =
            match delete with
            | Command.Request.Options.RequestId requestId ->
                match requests |> Array.tryFindIndex (fun x -> x.Id = requestId.Value) with
                | None ->
                    Error
                    <| Operation
                        { Message = $"{requestId} not found to delete."
                          Code = Some ErrorCodes.NotFound }
                | Some index ->
                    requests[index]
                    |> EmbassyAccess.Mapper.Request.toInternal
                    |> Result.map (fun request ->
                        let data = requests |> Array.removeAt index
                        (data, request))

        let execute ct operation storage =
            async {
                return
                    match ct |> notCanceled with
                    | true ->

                        storage
                        |> Query.Json.get Key
                        |> Result.bind (fun data ->
                            match operation with
                            | Command.Request.Create options -> data |> create options |> Result.map id
                            | Command.Request.Update options -> data |> update options |> Result.map id
                            | Command.Request.Delete options -> data |> delete options |> Result.map id)
                        |> Result.bind (fun (data, item) ->
                            storage |> Command.Json.save Key data |> Result.map (fun _ -> item))
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }
