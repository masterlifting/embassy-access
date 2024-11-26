module internal EA.Worker.Countries.Serbia

open Infrastructure.Domain
open Worker.Domain
open EA.Worker.Embassies

let private Belgrade =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Belgrade"
          Task = None },
        [ Russian.register () ]
    )

let Tasks =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Serbia"
          Task = None },
        [ Belgrade ]
    )
