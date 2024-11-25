module internal EA.Worker.Countries.Ireland

open Infrastructure.Domain
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Embassies

let private Dublin =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Dublin"
          Task = None },
        [ Russian.addTasks <| Ireland Dublin ]
    )

let Tasks =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Ireland"
          Task = None },
        [ Dublin ]
    )
