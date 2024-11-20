[<RequireQualifiedAccess>]
module EA.Telegram.Persistence.Command

open Infrastructure
open Persistence.Domain
open Web.Telegram.Domain
open EA.Telegram.Domain

module Chat =
    type Operation =
        | Create of Chat
        | CreateOrUpdate of Chat
        | Update of Chat
        | Delete of ChatId

    module InMemory =
        open EA.Telegram

        let create chat (chats: External.Chat array) =
            match chats |> Array.tryFindIndex (fun x -> x.Id = chat.Id.Value) with
            | Some _ ->
                Error
                <| Operation
                    { Message = $"{chat.Id} already exists."
                      Code = Some ErrorCodes.ALREADY_EXISTS }
            | None ->
                let data = chats |> Array.append [| Mapper.Chat.toExternal chat |]
                Ok(data, chat)

        let update chat (chats: External.Chat array) =
            match chats |> Array.tryFindIndex (fun x -> x.Id = chat.Id.Value) with
            | None ->
                Error
                <| Operation
                    { Message = $"{chat.Id} not found to update."
                      Code = Some ErrorCodes.NOT_FOUND }
            | Some index ->
                let data =
                    chats
                    |> Array.mapi (fun i x -> if i = index then Mapper.Chat.toExternal chat else x)

                Ok(data, chat)

        let delete (chatId: ChatId) (chats: External.Chat array) =
            match chats |> Array.tryFindIndex (fun x -> x.Id = chatId.Value) with
            | None ->
                Error
                <| Operation
                    { Message = $"{chatId} not found to delete."
                      Code = Some ErrorCodes.NOT_FOUND }
            | Some index ->
                chats[index]
                |> Mapper.Chat.toInternal
                |> Result.map (fun chat ->
                    let data = chats |> Array.removeAt index
                    (data, chat))

        let createOrUpdate chat (chats: External.Chat array) =
            match chats |> Array.tryFindIndex (fun x -> x.Id = chat.Id.Value) with
            | None -> create chat chats
            | Some _ -> update chat chats
