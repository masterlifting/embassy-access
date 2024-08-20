open Infrastructure
open Persistence
open Persistence.Domain
open Worker.Domain
open EmbassyAccess.Worker
open EmbassyAccess.Domain
open EmbassyAccess.Persistence

let private createRussianTestRequest ct (value, country) =
    Storage.create InMemory
    |> ResultAsync.wrap (fun storage ->
        let request =
            { Id = RequestId.New
              Payload = value
              Embassy = Russian country
              State = Created
              Attempt = 0
              ConfirmationState = Auto <| FirstAvailable
              Appointments = Set.empty
              Description = None
              GroupBy = None
              Modified = System.DateTime.UtcNow }

        storage
        |> Repository.Command.Request.create ct request
        |> ResultAsync.map (fun _ -> Success "Test request was created."))

[<EntryPoint>]
let main _ =

    let configuration = Configuration.getYaml "appsettings"
    Logging.useConsole configuration

    let rootTask =
        { Name = "Scheduler"
          Task =
            Some
            <| fun (_, _, ct) ->
                async {
                    let! testRequests =
                        [ ("https://berlin.kdmid.ru/queue/OrderInfo.aspx?id=298905&cd=9D217A77", Germany Berlin)
                          ("https://berlin.kdmid.ru/queue/OrderInfo.aspx?id=298907&cd=E66119D3", Germany Berlin)
                          ("https://belgrad.kdmid.ru/queue/orderinfo.aspx?id=72096&cd=7FE4D97C&ems=7EE040C9",
                           Serbia Belgrade)
                          //   ("https://belgrad.kdmid.ru/queue/orderinfo.aspx?id=72095&cd=68672FA8&ems=742D4A80",
                          //    Serbia Belgrade)
                          //   ("https://belgrad.kdmid.ru/queue/OrderInfo.aspx?id=91373&cd=7EE89A15", Serbia Belgrade)
                          ("https://sarajevo.kdmid.ru/queue/orderinfo.aspx?id=20779&cd=99CEBA38", Bosnia Sarajevo)
                          ("https://sarajevo.kdmid.ru/queue/orderinfo.aspx?id=20780&cd=4FC17A57", Bosnia Sarajevo)
                          ("https://sarajevo.kdmid.ru/queue/orderinfo.aspx?id=20781&cd=F23CB539", Bosnia Sarajevo)
                          ("https://hague.kdmid.ru/queue/orderinfo.aspx?id=114878&cd=f1e14d11&ems=2CAA46D6",
                           Netherlands Hague) ]
                        |> List.map (createRussianTestRequest ct)
                        |> Async.Sequential

                    return
                        testRequests
                        |> Seq.roe
                        |> Result.map (fun _ -> Success "Test requests were created. Scheduler has started...")
                } }

    let taskHandlers =
        Graph.Node(
            rootTask,
            [
              // Countries.Albania.Tasks
              //   Countries.Bosnia.Tasks
              //   Countries.Finland.Tasks
              //   Countries.France.Tasks
              //   Countries.Germany.Tasks
              //   Countries.Hungary.Tasks
              //   Countries.Ireland.Tasks
              //   Countries.Montenegro.Tasks
              //   Countries.Netherlands.Tasks
              Countries.Serbia.Tasks
              //   Countries.Slovenia.Tasks
              //   Countries.Switzerland.Tasks
              ]
        )

    "Scheduler"
    |> Worker.Core.start
        { getTask = taskHandlers |> TasksStorage.getTask configuration
          Configuration = configuration }
    |> Async.RunSynchronously

    0
