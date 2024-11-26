module internal EA.Worker.Countries.Netherlands

open Infrastructure.Domain
open Worker.Domain
open EA.Worker.Embassies

let private Hague =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Hague"
          Task = None },
        [ Russian.register () ]
    )

let Tasks =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Netherlands"
          Task = None },
        [ Hague ]
    )
