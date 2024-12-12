[<AutoOpen>]
module EA.Core.Domain.RequestService

type RequestService =
    { Name: string
      Payload: string
      Embassy: EmbassyNode
      Description: string option }
