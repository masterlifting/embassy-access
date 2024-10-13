module internal EA.Worker.Countries.Serbia

open Infrastructure.Domain
open Worker.Domain
open EA.Domain
open EA.Worker.Embassies

let private Belgrade =
    Graph.Node({ Name = "Belgrade"; Task = None }, [ Russian.addTasks <| Serbia Belgrade ])

let Tasks = Graph.Node({ Name = "Serbia"; Task = None }, [ Belgrade ])
