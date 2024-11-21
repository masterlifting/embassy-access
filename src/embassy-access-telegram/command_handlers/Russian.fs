module EA.Telegram.CommandHandler.Russian

let services () =
    EA.Embassies.Russian.Domain.Service.LIST
    
let service (embassy, name, level) =
    match level with
    | 0 -> EA.Embassies.Russian.Domain.Service.MAP |> Map.tryFind name
