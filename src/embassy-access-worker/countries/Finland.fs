module internal EA.Worker.Countries.Finland

open Infrastructure.Domain
open Worker.Domain
open EA.Worker.Embassies

let private Helsinki =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Helsinki"
          Task = None },
        [ Russian.register () ]
    )

let Tasks =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Finland"
          Task = None },
        [ Helsinki ]
    )
