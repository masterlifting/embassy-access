module internal EA.Worker.Countries.Ireland

open Infrastructure.Domain
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Embassies

let private Dublin =
    Graph.Node({ Name = "Dublin"; Task = None }, [ Russian.addTasks <| Ireland Dublin ])

let Tasks = Graph.Node({ Name = "Ireland"; Task = None }, [ Dublin ])
