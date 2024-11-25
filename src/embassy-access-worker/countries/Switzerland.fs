module internal EA.Worker.Countries.Switzerland

open Infrastructure.Domain
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Embassies

let private Bern =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Bern"
          Task = None },
        [ Russian.addTasks <| Switzerland Bern ]
    )

let Tasks =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Switzerland"
          Task = None },
        [ Bern ]
    )
