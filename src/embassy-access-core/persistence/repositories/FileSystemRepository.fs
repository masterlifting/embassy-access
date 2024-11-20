[<RequireQualifiedAccess>]
module internal EA.Persistence.FileSystemRepository

open Infrastructure
open Persistence.FileSystem
open EA.Core.Domain

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
                |> ResultAsync.bind (Seq.map EA.Core.Mapper.Request.toInternal >> Result.choose)
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
                    | ByEmbassy embassy -> data |> List.filter (fun x -> x.Service.Embassy = embassy)

                storage
                |> Query.Json.get
                |> ResultAsync.bind (Seq.map EA.Core.Mapper.Request.toInternal >> Result.choose)
                |> ResultAsync.map filter
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return

module Command =
    module Request =
        open EA.Persistence.Command.Request
        open EA.Persistence.Command.Request.InMemory

        let execute operation ct client =
            match ct |> notCanceled with
            | true ->

                client
                |> Query.Json.get
                |> ResultAsync.bind (fun data ->
                    match operation with
                    | Create request -> data |> create request |> Result.map id
                    | CreateOrUpdate request -> data |> createOrUpdate request |> Result.map id
                    | Update request -> data |> update request |> Result.map id
                    | Delete request -> data |> delete request |> Result.map id)
                |> ResultAsync.bindAsync (fun (data, item) ->
                    client |> Command.Json.save data |> ResultAsync.map (fun _ -> item))
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return
