[<RequireQualifiedAccess>]
module EA.Telegram.Persistence.Query

open EA.Domain
open EA.Telegram.Domain
open Web.Telegram.Domain

module Filter =
    module Chat =
        module InMemory =
            let hasSubscription (subId: RequestId) (chat: Chat) =
                chat.Subscriptions |> Set.contains subId

            let hasSubscriptions (subIds: RequestId seq) (chat: Chat) =
                let subIds = subIds |> Set.ofSeq

                chat.Subscriptions
                |> Seq.filter (fun subId -> subIds |> Seq.contains subId)
                |> Seq.tryHead
                |> Option.isSome

module Chat =
    type GetOne = ById of ChatId

    type GetMany =
        | BySubscription of RequestId
        | BySubscriptions of RequestId seq
