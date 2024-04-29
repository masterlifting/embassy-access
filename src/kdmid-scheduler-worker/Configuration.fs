module internal KdmidScheduler.Worker.Configuration

open Infrastructure

let AppSettings = Configuration.getJsonConfiguration "appsettings.json"
let getSection<'a> = AppSettings |> Configuration.getSection<'a>