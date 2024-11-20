[<RequireQualifiedAccess>]
module internal EA.Persistence.InMemoryRepository

open Infrastructure
open Persistence.InMemory
open EA.Core.Domain

module Query =
    module Request =
        open EA.Persistence.Query.Filter.Request
        open EA.Persistence.Query.Request

        let getOne query ct storage =
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
                        |> Query.Json.get Constants.REQUESTS_STORAGE_NAME
                        |> Result.bind (Seq.map EA.Core.Mapper.Request.toInternal >> Result.choose)
                        |> Result.map filter
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }

        let getMany query ct storage =
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
                            | ByIds requestIds -> data |> List.filter (fun x -> requestIds.Contains x.Id)
                            | ByEmbassy embassy -> data |> List.filter (fun x -> x.Service.Embassy = embassy)


                        storage
                        |> Query.Json.get Constants.REQUESTS_STORAGE_NAME
                        |> Result.bind (Seq.map EA.Core.Mapper.Request.toInternal >> Result.choose)
                        |> Result.map filter
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }

module Command =
    module Request =
        open EA.Persistence.Command.Request
        open EA.Persistence.Command.Request.InMemory

        let execute operation ct client =
            async {
                return
                    match ct |> notCanceled with
                    | true ->

                        client
                        |> Query.Json.get Constants.REQUESTS_STORAGE_NAME
                        |> Result.bind (fun data ->
                            match operation with
                            | Create request -> data |> create request |> Result.map id
                            | CreateOrUpdate request -> data |> createOrUpdate request |> Result.map id
                            | Update request -> data |> update request |> Result.map id
                            | Delete requestId -> data |> delete requestId |> Result.map id)
                        |> Result.bind (fun (data, item) ->
                            client
                            |> Command.Json.save Constants.REQUESTS_STORAGE_NAME data
                            |> Result.map (fun _ -> item))
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }
