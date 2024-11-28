[<RequireQualifiedAccess>]
module EA.Telegram.Persistence.Query

open EA.Core.Domain
open EA.Telegram.Domain
open Web.Telegram.Domain

module Chat =
    type TryFindOne = ById of ChatId

    type FindMany =
        | BySubscription of RequestId
        | BySubscriptions of RequestId seq

    module internal InMemory =

        module FindOne =
            let byId (id: ChatId) (chats: Chat list) =
                chats |> Seq.tryFind (fun x -> x.Id = id) |> Ok

        module FindMany =

            let bySubscription (requestId: RequestId) (chats: Chat list) =
                chats
                |> Seq.filter (fun x -> x.Subscriptions |> Set.exists (fun id -> id = requestId))
                |> Ok

            let bySubscriptions (requestIds: RequestId seq) (chats: Chat list) =
                let subscriptions = requestIds |> Set.ofSeq

                chats
                |> Seq.filter (fun x -> x.Subscriptions |> Set.intersect subscriptions |> Set.isEmpty |> not)
                |> Ok
