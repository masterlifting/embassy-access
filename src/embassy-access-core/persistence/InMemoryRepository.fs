[<RequireQualifiedAccess>]
module internal EmbassyAccess.Persistence.InMemoryRepository

open Infrastructure
open Persistence.Domain
open EmbassyAccess.Domain
open Persistence.InMemory

[<Literal>]
let private RequestsKey = "requests"

let private getEntities<'a> key context =
    context
    |> Storage.Query.get key
    |> Result.bind (Json.deserialize<'a array> |> Option.map >> Option.defaultValue (Ok [||]))

module Query =
    let paginate<'a> (pagination: Query.Pagination<'a>) (data: 'a list) =
        data
        |> match pagination.SortBy with
           | Query.Asc sortBy ->
               match sortBy with
               | Query.Date getValue -> List.sortBy <| getValue
               | Query.String getValue -> List.sortBy <| getValue
               | Query.Int getValue -> List.sortBy <| getValue
               | Query.Bool getValue -> List.sortBy <| getValue
               | Query.Guid getValue -> List.sortBy <| getValue
           | Query.Desc sortBy ->
               match sortBy with
               | Query.Date getValue -> List.sortByDescending <| getValue
               | Query.String getValue -> List.sortByDescending <| getValue
               | Query.Int getValue -> List.sortByDescending <| getValue
               | Query.Bool getValue -> List.sortByDescending <| getValue
               | Query.Guid getValue -> List.sortByDescending <| getValue
        |> List.skip (pagination.PageSize * (pagination.Page - 1))
        |> List.truncate pagination.PageSize

    module Request =

        module private Filters =
            let searchAppointments (filter: Query.SearchAppointmentsRequest) (request: Request) =
                filter.Embassy = request.Embassy
                && filter.HasStates request.State
                && filter.HasConfirmationState request.ConfirmationState

            let makeAppointments (filter: Query.MakeAppointmentRequest) (request: Request) =
                filter.Embassy = request.Embassy
                && filter.HasStates request.State
                && filter.HasConfirmationStates request.ConfirmationState

        let get ct (filter: Query.Request) context =
            async {
                return
                    match ct |> notCanceled with
                    | true ->
                        let filter (requests: Request list) =
                            match filter with
                            | Query.SearchAppointments embassy ->
                                let filter = Query.SearchAppointmentsRequest.create embassy

                                requests
                                |> List.filter (Filters.searchAppointments filter)
                                |> paginate filter.Pagination
                            | Query.MakeAppointments embassy ->
                                let filter = Query.MakeAppointmentRequest.create embassy

                                requests
                                |> List.filter (Filters.makeAppointments filter)
                                |> paginate filter.Pagination

                        context
                        |> getEntities<External.Request> RequestsKey
                        |> Result.bind (Seq.map EmbassyAccess.Mapper.Request.toInternal >> Result.choose)
                        |> Result.map filter
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }

        let get' ct requestId context =
            async {
                return
                    match ct |> notCanceled with
                    | true ->
                        context
                        |> getEntities<External.Request> RequestsKey
                        |> Result.bind (Seq.map EmbassyAccess.Mapper.Request.toInternal >> Result.choose)
                        |> Result.map (List.tryFind (fun x -> x.Id = requestId))
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }

module Command =

    let private save<'a> key (data: 'a array) context =
        if data.Length = 1 then
            data
            |> Json.serialize
            |> Result.bind (fun value -> context |> Storage.Command.add key value)
        else
            data
            |> Json.serialize
            |> Result.bind (fun value -> context |> Storage.Command.update key value)

    module Request =

        module Filters =
            let passportsRequest (embassy, payload) (data: External.Request seq) =
                let embassy = embassy |> EmbassyAccess.Mapper.Embassy.toExternal
                data |> Seq.exists (fun x -> x.Embassy = embassy && x.Payload = payload)

        let private create (options: Command.CreateOptions) (requests: External.Request array) =
            match options with
            | Command.CreateOptions.PassportsRequest value ->
                match requests |> Filters.passportsRequest (value.Embassy, value.Payload) with
                | true ->
                    Error
                    <| Operation
                        { Message = $"Request for {value.Embassy} with {value.Payload} already exists."
                          Code = Some ErrorCodes.AlreadyExists }
                | _ ->
                    let request = value.create ()

                    match value.Validation with
                    | Some validate -> request |> validate
                    | _ -> Ok()
                    |> Result.map (fun _ ->
                        let data =
                            requests |> Array.append [| EmbassyAccess.Mapper.Request.toExternal request |]

                        (data, request))

        let private update (request: Request) (requests: External.Request array) =
            match requests |> Array.tryFindIndex (fun x -> x.Id = request.Id.Value) with
            | None ->
                Error
                <| Operation
                    { Message = $"{request.Id} not found to update."
                      Code = Some ErrorCodes.NotFound }
            | Some index ->
                Ok(
                    requests
                    |> Array.mapi (fun i x ->
                        if i = index then
                            EmbassyAccess.Mapper.Request.toExternal request
                        else
                            x)
                )

        let private delete (request: Request) (requests: External.Request array) =
            match requests |> Array.tryFindIndex (fun x -> x.Id = request.Id.Value) with
            | None ->
                Error
                <| Operation
                    { Message = $"{request.Id} not found to delete."
                      Code = Some ErrorCodes.NotFound }
            | Some index -> Ok(requests |> Array.removeAt index)

        let execute ct command context =
            async {
                return
                    match ct |> notCanceled with
                    | true ->

                        context
                        |> getEntities<External.Request> RequestsKey
                        |> Result.bind (fun requests ->
                            match command with
                            | Command.Request.Create request -> requests |> create request |> Result.map id
                            | Command.Request.Update request ->
                                requests |> update request |> Result.map (fun result -> result, request)
                            | Command.Request.Delete request ->
                                requests |> delete request |> Result.map (fun result -> result, request))
                        |> Result.bind (fun (result, request) ->
                            context |> save RequestsKey result |> Result.map (fun _ -> request))
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }
