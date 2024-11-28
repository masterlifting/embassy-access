[<RequireQualifiedAccess>]
module internal EA.Persistence.InMemoryRepository

open Infrastructure
open Persistence.InMemory
open EA.Core.Domain

module Query =
    module Request =
        open EA.Core.Persistence.Query.Request

        let tryFindOne query ct storage =
            async {
                return
                    match ct |> notCanceled with
                    | true ->
                        let filter (data: Request list) =
                            match query with
                            | ById requestId -> data |> InMemory.FindOne.byId requestId
                            | FirstByName embassyName -> data |> InMemory.FindOne.first embassyName
                            | SingleByName serviceName -> data |> InMemory.FindOne.single serviceName

                        storage
                        |> Query.Json.get Constants.REQUESTS_STORAGE_NAME
                        |> Result.bind (Seq.map EA.Core.Mapper.Request.toInternal >> Result.choose)
                        |> Result.bind filter
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }

        let findMany query ct storage =
            async {
                return
                    match ct |> notCanceled with
                    | true ->
                        let filter (data: Request list) =
                            match query with
                            | ByIds requestIds -> data |> InMemory.FindMany.byIds requestIds
                            | ByEmbassyName embassyName -> data |> InMemory.FindMany.byEmbassyName embassyName

                        storage
                        |> Query.Json.get Constants.REQUESTS_STORAGE_NAME
                        |> Result.bind (Seq.map EA.Core.Mapper.Request.toInternal >> Result.choose)
                        |> Result.bind filter
                        |> Result.map List.ofSeq
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }

module Command =
    module Request =
        open EA.Core.Persistence.Command.Request

        let execute operation ct client =
            async {
                return
                    match ct |> notCanceled with
                    | true ->

                        client
                        |> Query.Json.get Constants.REQUESTS_STORAGE_NAME
                        |> Result.bind (fun data ->
                            match operation with
                            | Create request -> data |> InMemory.create request |> Result.map id
                            | CreateOrUpdate request -> data |> InMemory.createOrUpdate request |> Result.map id
                            | Update request -> data |> InMemory.update request |> Result.map id
                            | Delete requestId -> data |> InMemory.delete requestId |> Result.map id)
                        |> Result.bind (fun (data, item) ->
                            client
                            |> Command.Json.save Constants.REQUESTS_STORAGE_NAME data
                            |> Result.map (fun _ -> item))
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }
