module internal EA.Worker.Countries.Bosnia

open Infrastructure.Domain
open Worker.Domain
open EA.Core.Domain
open EA.Worker.Embassies

let private Sarajevo =
    Graph.Node({ Name = "Sarajevo"; Task = None }, [ Russian.addTasks <| Bosnia Sarajevo ])

let Tasks = Graph.Node({ Name = "Bosnia"; Task = None }, [ Sarajevo ])
