[<AutoOpen>]
module EA.Telegram.Domain.Subscription

open EA.Core.Domain

type Subscription = {
    Id: RequestId
    ServiceId: ServiceId
    EmbassyId: EmbassyId
}
