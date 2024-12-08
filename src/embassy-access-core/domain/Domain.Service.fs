[<AutoOpen>]
module EA.Core.Domain.Service

type Service =
    { Name: string
      Payload: string
      Embassy: EmbassyGraph
      Description: string option }
