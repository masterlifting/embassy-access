module internal EmbassyAccess.Worker.Countries.Montenegro

open Infrastructure.Domain
open Worker.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Worker.Embassies

let private Podgorica =
    Graph.Node({ Name = "Podgorica"; Task = None }, [ Russian.addTasks <| Montenegro Podgorica ])

let Tasks = Graph.Node({ Name = "Montenegro"; Task = None }, [ Podgorica ])
