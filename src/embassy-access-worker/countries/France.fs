module internal EA.Worker.Countries.France

open Infrastructure.Domain
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Embassies

let private Paris =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "Paris"
          Task = None },
        [ Russian.addTasks <| France Paris ]
    )

let Tasks =
    Graph.Node(
        { Id = Graph.NodeId.New
          Name = "France"
          Task = None },
        [ Paris ]
    )
