module internal EA.Worker.Countries.Slovenia

open Infrastructure.Domain
open Worker.Domain
open EA.Worker.Embassies

let private Ljubljana =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Ljubljana"
          Task = None },
        [ Russian.register () ]
    )

let Tasks =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Slovenia"
          Task = None },
        [ Ljubljana ]
    )
