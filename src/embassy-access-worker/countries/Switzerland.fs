module internal EA.Worker.Countries.Switzerland

open Infrastructure.Domain
open Worker.Domain
open EA.Domain
open EA.Worker.Embassies

let private Bern =
    Graph.Node({ Name = "Bern"; Task = None }, [ Russian.addTasks <| Switzerland Bern ])

let Tasks = Graph.Node({ Name = "Switzerland"; Task = None }, [ Bern ])
