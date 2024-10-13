module internal EA.Worker.Countries.Albania

open Infrastructure.Domain
open Worker.Domain
open EA.Domain
open EA.Worker.Embassies

let private Tirana =
    Graph.Node({ Name = "Tirana"; Task = None }, [ Russian.addTasks <| Albania Tirana ])

let Tasks = Graph.Node({ Name = "Albania"; Task = None }, [ Tirana ])
