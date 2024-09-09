[<RequireQualifiedAccess>]
module EmbassyAccess.Notification.Receive

open EmbassyAccess

type Request = Content of Web.Domain.ReceiveData<string>
