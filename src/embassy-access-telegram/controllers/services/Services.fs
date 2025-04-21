[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Services

open Infrastructure.Prelude
open EA.Telegram.Router.Services
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Services.Russian
open EA.Telegram.Dependencies.Services.Italian
open EA.Telegram.Services.Services

let respond request chat =
    fun (deps: Request.Dependencies) ->
        deps
        |> Services.Dependencies.create chat
        |> ResultAsync.wrap (fun deps ->

            match request with
            | Method.Russian russian ->
                match russian with
                | Russian.Method.Get get ->
                    Russian.Dependencies.create deps
                    |> ResultAsync.wrap (fun deps ->
                        match get with
                        | Get.Services embassyId -> deps |> Russian.Query.getServices embassyId
                        | Get.Service(embassyId, serviceId) -> deps |> Russian.Query.getService embassyId serviceId
                        | Get.UserServices embassyId -> deps |> Russian.Query.getUserServices embassyId
                        | Get.UserService(embassyId, serviceId) -> deps |> Russian.Query.getUserService embassyId serviceId)
            | Method.Italian italian ->
                match italian with
                | Italian.Method.Get get ->
                    Italian.Dependencies.create deps
                    |> ResultAsync.wrap (fun deps ->
                        match get with
                        | Get.Services embassyId -> deps |> Italian.Query.getServices embassyId
                        | Get.Service(embassyId, serviceId) -> deps |> Italian.Query.getService embassyId serviceId
                        | Get.UserServices embassyId -> deps |> Italian.Query.getUserServices embassyId
                        | Get.UserService(embassyId, serviceId) -> deps |> Italian.Query.getUserService embassyId serviceId))
