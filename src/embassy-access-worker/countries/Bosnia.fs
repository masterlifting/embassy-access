module internal EmbassyAccess.Worker.Countries.Bosnia

open Infrastructure.Domain
open Worker.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Worker.Embassies

let private Sarajevo =
    Graph.Node({ Name = "Sarajevo"; Task = None }, [ Russian.addTasks <| Bosnia Sarajevo ])

let Tasks = Graph.Node({ Name = "Bosnia"; Task = None }, [ Sarajevo ])
