[<RequireQualifiedAccess>]
module internal EA.Persistence.FileSystemRepository

open Infrastructure
open Persistence.FileSystem
open EA.Core.Domain

module Query =
    module Request =
        open EA.Core.Persistence.Query.Request

        let trytFindOne query ct storage =
            match ct |> notCanceled with
            | true ->
                let filter (data: Request list) =
                    match query with
                    | ById requestId -> data |> InMemory.FindOne.byId requestId
                    | FirstByName embassyName -> data |> InMemory.FindOne.first embassyName
                    | SingleByName serviceName -> data |> InMemory.FindOne.single serviceName


                storage
                |> Query.Json.get
                |> ResultAsync.bind (Seq.map EA.Core.Mapper.Request.toInternal >> Result.choose)
                |> ResultAsync.bind filter
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return

        let findMany query ct storage =
            match ct |> notCanceled with
            | true ->
                let filter (data: Request list) =
                    match query with
                    | ByIds requestIds -> data |> InMemory.FindMany.byIds requestIds
                    | ByEmbassyName embassyName -> data |> InMemory.FindMany.byEmbassyName embassyName

                storage
                |> Query.Json.get
                |> ResultAsync.bind (Seq.map EA.Core.Mapper.Request.toInternal >> Result.choose)
                |> ResultAsync.bind filter
                |> ResultAsync.map List.ofSeq
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return

module Command =
    module Request =
        open EA.Core.Persistence.Command.Request

        let execute operation ct client =
            match ct |> notCanceled with
            | true ->

                client
                |> Query.Json.get
                |> ResultAsync.bind (fun data ->
                    match operation with
                    | Create request -> data |> InMemory.create request |> Result.map id
                    | CreateOrUpdate request -> data |> InMemory.createOrUpdate request |> Result.map id
                    | Update request -> data |> InMemory.update request |> Result.map id
                    | Delete request -> data |> InMemory.delete request |> Result.map id)
                |> ResultAsync.bindAsync (fun (data, item) ->
                    client |> Command.Json.save data |> ResultAsync.map (fun _ -> item))
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return
