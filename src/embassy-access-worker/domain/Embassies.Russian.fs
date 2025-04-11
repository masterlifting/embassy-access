module EA.Worker.Domain.Embassies.Russian

open Infrastructure.Domain

type KdmidSubdomain = {
    Name: string
    EmbassyId: Graph.NodeId
}
