[<AutoOpen>]
module EA.Core.Domain.Service

type Service =
    { Name: string
      Payload: string
      Embassy: Embassy
      Description: string option }
