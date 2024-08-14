module internal EmbassyAccess.Worker.Countries.Germany

open Infrastructure.Domain
open Worker.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Worker.Embassies

let private Berlin =
    Graph.Node({ Name = "Berlin"; Task = None }, [ Russian.addTasks <| Germany Berlin ])

let Tasks = Graph.Node({ Name = "Germany"; Task = None }, [ Berlin ])