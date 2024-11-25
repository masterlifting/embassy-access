module internal EA.Worker.Countries.Albania

open Infrastructure.Domain
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Embassies

let private Tirana =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Tirana"
          Task = None },
        [ Russian.addTasks <| Albania Tirana ]
    )

let Tasks =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Albania"
          Task = None },
        [ Tirana ]
    )
