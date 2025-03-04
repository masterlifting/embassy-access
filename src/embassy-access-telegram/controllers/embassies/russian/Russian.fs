[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.Russian.Russian

open Infrastructure.Prelude
open EA.Telegram.Domain
open EA.Telegram.Endpoints.Embassies.Russian
open EA.Telegram.Endpoints.Embassies.Russian.Request
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Dependencies.Consumer.Embassies.Russian
open EA.Telegram.Services.Culture

let respond request chat =
    fun (deps: Consumer.Dependencies) ->
        let translate msgRes =
            deps |> Command.translateRes chat.Culture msgRes

        let translateSeq msgSeqRes =
            deps |> Command.translateSeqRes chat.Culture msgSeqRes

        Russian.Dependencies.create chat (translate, translateSeq) deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get(Get.Kdmid get) -> deps |> Kdmid.get get
            | Get(Get.Midpass get) -> deps |> Midpass.get get
            | Post(Post.Kdmid model) -> deps |> Kdmid.post model
            | Delete(Delete.Kdmid delete) -> deps |> Kdmid.delete delete)
