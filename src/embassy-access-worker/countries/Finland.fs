module internal EmbassyAccess.Worker.Countries.Finland

open Infrastructure.Domain
open Worker.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Worker.Embassies

let private Helsinki =
    Graph.Node({ Name = "Helsinki"; Task = None }, [ Russian.addTasks <| Finland Helsinki ])

let Tasks = Graph.Node({ Name = "Finland"; Task = None }, [ Helsinki ])
