module internal EA.Worker.Countries.Netherlands

open Infrastructure.Domain
open Worker.Domain
open EA.Domain
open EA.Worker.Embassies

let private Hague =
    Graph.Node({ Name = "Hague"; Task = None }, [ Russian.addTasks <| Netherlands Hague ])

let Tasks = Graph.Node({ Name = "Netherlands"; Task = None }, [ Hague ])
