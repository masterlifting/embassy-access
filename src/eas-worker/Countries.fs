﻿module internal Eas.Worker.Countries

open Infrastructure.Domain.Graph
open Infrastructure.DSL
open Worker.Domain.Internal
open Eas.Domain.Internal
open Eas.Persistence

module Serbia =
    open Persistence.Storage.Core
    open Persistence.Domain.Core

    let private createTestRequest =
        fun _ ct ->
            createStorage InMemory
            |> ResultAsync.wrap (fun storage ->
                let request =
                    { Id = System.Guid.NewGuid() |> RequestId
                      Embassy = Russian <| Serbia Belgrade
                      Data = Map [ "url", "https://berlin.kdmid.ru/queue/orderinfo.aspx?id=290383&cd=B714253F" ]
                      Modified = System.DateTime.UtcNow }

                storage
                |> Repository.Command.Request.create ct request
                |> ResultAsync.map (fun _ -> Success "Test request was created."))

    let private Belgrade =
        Node(
            { Name = "Belgrade"
              Handle = Some <| createTestRequest },
            [ Embassies.Russian.createNode <| Serbia Belgrade ]
        )

    let Node = Node({ Name = "Serbia"; Handle = None }, [ Belgrade ])

module Bosnia =
    let private Sarajevo =
        Node({ Name = "Sarajevo"; Handle = None }, [ Embassies.Russian.createNode <| Bosnia Sarajevo ])

    let Node = Node({ Name = "Bosnia"; Handle = None }, [ Sarajevo ])

module Montenegro =
    let private Podgorica =
        Node({ Name = "Podgorica"; Handle = None }, [ Embassies.Russian.createNode <| Montenegro Podgorica ])

    let Node = Node({ Name = "Montenegro"; Handle = None }, [ Podgorica ])

module Albania =
    let private Tirana =
        Node({ Name = "Tirana"; Handle = None }, [ Embassies.Russian.createNode <| Albania Tirana ])

    let Node = Node({ Name = "Albania"; Handle = None }, [ Tirana ])

module Hungary =
    let private Budapest =
        Node({ Name = "Budapest"; Handle = None }, [ Embassies.Russian.createNode <| Hungary Budapest ])

    let Node = Node({ Name = "Hungary"; Handle = None }, [ Budapest ])
