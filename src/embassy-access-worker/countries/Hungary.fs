module internal EmbassyAccess.Worker.Countries.Hungary

open Infrastructure.Domain
open Worker.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Worker.Embassies

let private Budapest =
    Graph.Node({ Name = "Budapest"; Task = None }, [ Russian.addTasks <| Hungary Budapest ])

let Tasks = Graph.Node({ Name = "Hungary"; Task = None }, [ Budapest ])