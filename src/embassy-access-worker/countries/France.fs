module internal EA.Worker.Countries.France

open Infrastructure.Domain
open Worker.Domain
open EA.Domain
open EA.Worker.Embassies

let private Paris =
    Graph.Node({ Name = "Paris"; Task = None }, [ Russian.addTasks <| France Paris ])

let Tasks = Graph.Node({ Name = "France"; Task = None }, [ Paris ])
