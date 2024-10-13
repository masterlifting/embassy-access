module internal EA.Worker.Countries.Hungary

open Infrastructure.Domain
open Worker.Domain
open EA.Domain
open EA.Worker.Embassies

let private Budapest =
    Graph.Node({ Name = "Budapest"; Task = None }, [ Russian.addTasks <| Hungary Budapest ])

let Tasks = Graph.Node({ Name = "Hungary"; Task = None }, [ Budapest ])
