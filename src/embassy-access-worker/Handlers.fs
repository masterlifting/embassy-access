module internal EA.Worker.Handlers

open Infrastructure.Domain
open Infrastructure.Prelude.Tree.Builder
open EA.Worker

let register () =
    Tree.Node.create ("WRK", Some Initializer.run)
    |> withChildren [

        Tree.Node.create ("RUS", None)
        |> withChild (
            Tree.Node.create ("SRB", None)
            |> withChild (Tree.Node.create ("SA", Some Embassies.Russian.Kdmid.SearchAppointments.handle))
        )

        Tree.Node.create ("ITA", None)
        |> withChild (
            Tree.Node.create ("SRB", None)
            |> withChild (Tree.Node.create ("SA", Some Embassies.Italian.Prenotami.SearchAppointments.handle))
        )
    ]
