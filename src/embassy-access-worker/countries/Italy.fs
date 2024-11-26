module internal EA.Worker.Countries.Italy

open Infrastructure.Domain
open Worker.Domain
open EA.Worker.Embassies

let private Rome =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Rome"
          Task = None },
        [ Russian.register () ]
    )

let Tasks =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Italy"
          Task = None },
        [ Rome ]
    )
