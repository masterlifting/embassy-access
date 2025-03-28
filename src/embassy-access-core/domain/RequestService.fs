[<AutoOpen>]
module EA.Core.Domain.RequestService

open Infrastructure.Domain

type RequestService = {
    Id: Graph.NodeId
    Name: string
    Payload: string
    Description: string option
    Embassy: EmbassyNode
}
