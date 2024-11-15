open System
open Expecto

let private tests =
    testList "EA.Embassies.Russian.Tests" [ EA.Embassies.Russian.Kdmid.Tests.list ]

[<EntryPoint>]
let main args =
    Console.OutputEncoding <- Text.Encoding.UTF8
    runTestsWithCLIArgs [] args tests
