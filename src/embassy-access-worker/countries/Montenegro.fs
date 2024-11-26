module internal EA.Worker.Countries.Montenegro

open Infrastructure.Domain
open Worker.Domain
open EA.Worker.Embassies

let private Podgorica =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Podgorica"
          Task = None },
        [ Russian.register () ]
    )

let Tasks =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Montenegro"
          Task = None },
        [ Podgorica ]
    )
