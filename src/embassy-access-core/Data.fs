module EA.Core.Data

open Infrastructure
open EA.Core.Domain


module Embassies =
    let RUSSIAN =
        { Id = "b40ac8a5-4a58-4869-80a2-680fd3fea745" |> Graph.NodeId.create
          Name = "Russian" }

    let SPANISH =
        { Id = "e571ef38-4a58-4997-ae7b-e47110e7a5f9" |> Graph.NodeId.create
          Name = "Spanish" }

    let ITALIAN =
        { Id = "25041988-8737-4f7e-9726-ae8c6a6c8ce5" |> Graph.NodeId.create
          Name = "Italian" }

    let GERMAN =
        { Id = "b471ebf3-27b8-4f0d-bce1-9db16d031ec7" |> Graph.NodeId.create
          Name = "German" }

    let FRENCH =
        { Id = "a26cf28a-2fc3-4909-bd83-dcb034c63d37" |> Graph.NodeId.create
          Name = "French" }

    let BRITISH =
        { Id = "0690e4a9-f46b-4657-8e46-f0900ad0c558" |> Graph.NodeId.create
          Name = "British" }

module Countries =
    let SERBIA =
        { Id = "0f74c89d-f67b-4fd9-9e41-4c1fbe3e6851" |> Graph.NodeId.create
          Name = "Serbia" }

    let GERMANY =
        { Id = "1a64e4f8-dc58-4781-bb65-e7b3f347e9d2" |> Graph.NodeId.create
          Name = "Germany" }

    let BOSNIA =
        { Id = "275ddc13-6d4b-45ef-bcb3-234a5b5b74e2" |> Graph.NodeId.create
          Name = "Bosnia" }

    let MONTENEGRO =
        { Id = "3f5b9c85-7d56-482c-8fd6-49dc75a1c847" |> Graph.NodeId.create
          Name = "Montenegro" }

    let ALBANIA =
        { Id = "4673bafd-4d2e-4c83-9e68-b14b7c3d59a5" |> Graph.NodeId.create
          Name = "Albania" }

    let HUNGARY =
        { Id = "5f78b9d1-9a35-4af6-8d23-12c5ab7f9b34" |> Graph.NodeId.create
          Name = "Hungary" }

    let IRELAND =
        { Id = "6b25f134-df43-4ec7-a9c2-94b3c75a85f6" |> Graph.NodeId.create
          Name = "Ireland" }

    let ITALY =
        { Id = "73f14c7b-1b56-4a91-8fe2-28dcbf347e8a" |> Graph.NodeId.create
          Name = "Italy" }

    let SWITZERLAND =
        { Id = "8d2451b6-3a84-4d25-b4f7-49c7a51b8e4c" |> Graph.NodeId.create
          Name = "Switzerland" }

    let FINLAND =
        { Id = "97e45d3a-c82b-421f-bbe4-7c9b5e8d1f25" |> Graph.NodeId.create
          Name = "Finland" }

    let FRANCE =
        { Id = "a1d59c7e-4d23-4fd5-a3b7-dcb9f3478a5e" |> Graph.NodeId.create
          Name = "France" }

    let NETHERLANDS =
        { Id = "b7f45e8d-4a23-41fd-8e92-5c9b3f8d1f25" |> Graph.NodeId.create
          Name = "Netherlands" }

    let SLOVENIA =
        { Id = "c8d51b34-4f25-4a8b-9e7d-4d9f3c75a1b8" |> Graph.NodeId.create
          Name = "Slovenia" }

module Cities =
    let BELGRADE =
        { Id = "e1f989e4-49d3-4f5c-bd1c-8765c15a8c4a" |> Graph.NodeId.create
          Name = "Belgrade" }

    let BERLIN =
        { Id = "07c451b6-4686-4e27-8a8e-3c39a5a5e1b7" |> Graph.NodeId.create
          Name = "Berlin" }

    let BUDAPEST =
        { Id = "99a7d85d-e8d1-4c82-8a2e-b3a67c674edc" |> Graph.NodeId.create
          Name = "Budapest" }

    let SARAJEVO =
        { Id = "caa2b8b6-4587-4e82-91f6-2cd8bfddba6a" |> Graph.NodeId.create
          Name = "Sarajevo" }

    let PODGORICA =
        { Id = "d68f57a3-2be5-4d72-b4f6-7ea6d2427d9e" |> Graph.NodeId.create
          Name = "Podgorica" }

    let TIRANA =
        { Id = "c6e3729d-274d-42a1-8f0d-e56a8d7cfb92" |> Graph.NodeId.create
          Name = "Tirana" }

    let PARIS =
        { Id = "57d146a5-1b83-4c7e-906f-980759d1c1c4" |> Graph.NodeId.create
          Name = "Paris" }

    let ROME =
        { Id = "8949bd59-2c71-4d4b-bb72-b32cdd1af5b5" |> Graph.NodeId.create
          Name = "Rome" }

    let DUBLIN =
        { Id = "25714d1c-79b1-42dc-9a2c-4e7e3135d349" |> Graph.NodeId.create
          Name = "Dublin" }

    let BERN =
        { Id = "af37d614-d8b4-4856-b3d9-90f94677c1a8" |> Graph.NodeId.create
          Name = "Bern" }

    let HELSINKI =
        { Id = "c8db9c9e-3ef3-4eb2-b30b-1d8dbcf1e2c5" |> Graph.NodeId.create
          Name = "Helsinki" }

    let HAGUE =
        { Id = "e6dc1234-2347-4a9f-85f5-2fc8c63a5f78" |> Graph.NodeId.create
          Name = "Hague" }

    let LJUBLJANA =
        { Id = "f7d345ab-1a2c-4b78-a9ed-bc98f347e8a9" |> Graph.NodeId.create
          Name = "Ljubljana" }

let GRAPH =
    Graph.Node(
        { Id = "42274f3a-31c5-4cfc-a627-1cc37af60c0a" |> Graph.NodeId.create
          Name = "Core Data" },
        [ Graph.Node(
              Embassies.RUSSIAN,
              [ Graph.Node(Countries.SERBIA, [ Graph.Node(Cities.BELGRADE, []) ])
                Graph.Node(Countries.BOSNIA, [ Graph.Node(Cities.SARAJEVO, []) ])
                Graph.Node(Countries.MONTENEGRO, [ Graph.Node(Cities.PODGORICA, []) ])
                Graph.Node(Countries.ALBANIA, [ Graph.Node(Cities.TIRANA, []) ])
                Graph.Node(Countries.HUNGARY, [ Graph.Node(Cities.BUDAPEST, []) ]) ]
          )
          Graph.Node(
              Embassies.SPANISH,
              [ Graph.Node(Countries.SLOVENIA, [ Graph.Node(Cities.LJUBLJANA, []) ])
                Graph.Node(Countries.SWITZERLAND, [ Graph.Node(Cities.BERN, []) ])
                Graph.Node(Countries.NETHERLANDS, [ Graph.Node(Cities.HAGUE, []) ]) ]
          )
          Graph.Node(
              Embassies.ITALIAN,
              [ Graph.Node(Countries.ITALY, [ Graph.Node(Cities.ROME, []) ])
                Graph.Node(Countries.FRANCE, [ Graph.Node(Cities.PARIS, []) ]) ]
          )
          Graph.Node(Embassies.GERMAN, [ Graph.Node(Countries.GERMANY, [ Graph.Node(Cities.BERLIN, []) ]) ])
          Graph.Node(Embassies.FRENCH, [ Graph.Node(Countries.FRANCE, [ Graph.Node(Cities.PARIS, []) ]) ])
          Graph.Node(
              Embassies.BRITISH,
              [ Graph.Node(Countries.IRELAND, [ Graph.Node(Cities.DUBLIN, []) ])
                Graph.Node(Countries.FINLAND, [ Graph.Node(Cities.HELSINKI, []) ]) ]
          ) ]
    )
