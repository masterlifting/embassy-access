module internal EA.Worker.Countries.Hungary

open Infrastructure.Domain
open Worker.Domain
open EA.Worker.Embassies

let private Budapest =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Budapest"
          Task = None },
        [ Russian.register () ]
    )

let Tasks =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Hungary"
          Task = None },
        [ Budapest ]
    )
