module internal EA.Worker.Countries.Germany

open Infrastructure.Domain
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Embassies

let private Berlin =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Berlin"
          Task = None },
        [ Russian.addTasks <| Germany Berlin ]
    )

let Tasks =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Germany"
          Task = None },
        [ Berlin ]
    )
