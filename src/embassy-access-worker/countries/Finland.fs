module internal EA.Worker.Countries.Finland

open Infrastructure.Domain
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Embassies

let private Helsinki =
    Graph.Node({ Name = "Helsinki"; Task = None }, [ Russian.addTasks <| Finland Helsinki ])

let Tasks = Graph.Node({ Name = "Finland"; Task = None }, [ Helsinki ])
