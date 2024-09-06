[<RequireQualifiedAccess>]
module EmbassyAccess.Web.Response

open EmbassyAccess

type Request = Content of Web.Domain.Response<string>
