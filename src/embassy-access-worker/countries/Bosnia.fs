module internal EA.Worker.Countries.Bosnia

open Infrastructure.Domain
open Worker.Domain
open EA.Worker.Embassies

let private Sarajevo =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Sarajevo"
          Task = None },
        [ Russian.register () ]
    )

let Tasks =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Bosnia"
          Task = None },
        [ Sarajevo ]
    )
