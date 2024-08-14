module internal EmbassyAccess.Worker.Countries.Ireland

open Infrastructure.Domain
open Worker.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Worker.Embassies

let private Dublin =
    Graph.Node({ Name = "Dublin"; Task = None }, [ Russian.addTasks <| Ireland Dublin ])

let Tasks = Graph.Node({ Name = "Ireland"; Task = None }, [ Dublin ])