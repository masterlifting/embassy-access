module internal EmbassyAccess.Worker.Countries.Slovenia

open Infrastructure.Domain
open Worker.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Worker.Embassies

let private Ljubljana =
    Graph.Node({ Name = "Ljubljana"; Task = None }, [ Russian.addTasks <| Slovenia Ljubljana ])

let Tasks = Graph.Node({ Name = "Slovenia"; Task = None }, [ Ljubljana ])