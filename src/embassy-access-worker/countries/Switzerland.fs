module internal EmbassyAccess.Worker.Countries.Switzerland

open Infrastructure.Domain
open Worker.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Worker.Embassies

let private Bern =
    Graph.Node({ Name = "Bern"; Task = None }, [ Russian.addTasks <| Switzerland Bern ])

let Tasks = Graph.Node({ Name = "Switzerland"; Task = None }, [ Bern ])
