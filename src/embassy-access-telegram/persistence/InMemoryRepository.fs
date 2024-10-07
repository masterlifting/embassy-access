[<RequireQualifiedAccess>]
module internal EmbassyAccess.Telegram.Persistence.InMemoryRepository

open Infrastructure
open EmbassyAccess.Telegram
open EmbassyAccess.Telegram.Domain
open EmbassyAccess.Domain
open Persistence.InMemory
open Persistence.Domain

[<Literal>]
let private RequestsKey = "requests"

let private getEntities<'a> key context =
    context
    |> Storage.Query.get key
    |> Result.bind (Json.deserialize<'a array> |> Option.map >> Option.defaultValue (Ok [||]))

module Query =

    module Chat =

        module private Filters =
            let search (requestId: RequestId) (chat: Chat) =
                chat.Subscriptions |> Set.contains requestId

        let get ct (filter: Filter.Chats) context =
            async {
                return
                    match ct |> notCanceled with
                    | true ->
                        let filter (chats: Chat list) =
                            match filter with
                            | Filter.Search requestId -> chats |> List.filter (Filters.search requestId)

                        context
                        |> getEntities<External.Chat> RequestsKey
                        |> Result.bind (Seq.map Mapper.Chat.toInternal >> Result.choose)
                        |> Result.map filter
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

    module Chat =

        let private add (chat: Chat) (chats: External.Chat array) =
            match chats |> Array.tryFind (fun x -> x.Id = chat.Id) with
            | Some _ ->
                Error
                <| Operation
                    { Message = $"{chat.Id} already exists."
                      Code = Some ErrorCodes.AlreadyExists }
            | _ -> Ok(chats |> Array.append [| Mapper.Chat.toExternal chat |])

        let private update (chat: Chat) (chats: External.Chat array) =
            match chats |> Array.tryFindIndex (fun x -> x.Id = chat.Id) with
            | None ->
                Error
                <| Operation
                    { Message = $"{chat.Id} not found to update."
                      Code = Some ErrorCodes.NotFound }
            | Some index ->
                Ok(
                    chats
                    |> Array.mapi (fun i x -> if i = index then Mapper.Chat.toExternal chat else x)
                )

        let private delete (chat: Chat) (chats: External.Chat array) =
            match chats |> Array.tryFindIndex (fun x -> x.Id = chat.Id) with
            | None ->
                Error
                <| Operation
                    { Message = $"{chat.Id} not found to delete."
                      Code = Some ErrorCodes.NotFound }
            | Some index -> Ok(chats |> Array.removeAt index)

        let execute ct command context =
            async {
                return
                    match ct |> notCanceled with
                    | true ->

                        context
                        |> getEntities<External.Chat> RequestsKey
                        |> Result.bind (fun chats ->
                            match command with
                            | Command.Chat.Create chat -> chats |> add chat |> Result.map (fun result -> result, chat)
                            | Command.Chat.Update chat ->
                                chats |> update chat |> Result.map (fun result -> result, chat)
                            | Command.Chat.Delete chat ->
                                chats |> delete chat |> Result.map (fun result -> result, chat))
                        |> Result.bind (fun (result, chat) ->
                            context |> save RequestsKey result |> Result.map (fun _ -> chat))
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }
