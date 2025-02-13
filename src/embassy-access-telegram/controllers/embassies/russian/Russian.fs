[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.Russian.Russian

open Infrastructure.Prelude
open EA.Telegram.Endpoints.Embassies.Russian
open EA.Telegram.Endpoints.Embassies.Russian.Request
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Dependencies.Consumer.Embassies.Russian

let respond request =
    fun (deps: Consumer.Dependencies) ->
        Russian.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get(Get.Kdmid get) -> deps |> Kdmid.get get
            | Get(Get.Midpass get) -> deps |> Midpass.get get
            | Post(Post.Kdmid model) -> deps |> Kdmid.post model
            | Delete(Delete.Kdmid delete) -> deps |> Kdmid.delete delete)
