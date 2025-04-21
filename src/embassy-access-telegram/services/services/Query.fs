module EA.Telegram.Services.Services.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Services.Russian
open EA.Telegram.Dependencies.Services.Italian

let private tryParse (embassyId: EmbassyId) =
    embassyId.Value.Split()
    |> Seq.skip 1
    |> Seq.tryHead

let getService embassyId =
    fun (deps: Services.Dependencies) ->
        match tryParse embassyId with
        | Some embassy ->
            match embassy with
            | Embassies.RUS ->
                Russian.Dependencies.create deps
                |> ResultAsync.wrap (Russian.Query.getServices embassyId)
            | Embassies.ITA ->
                Italian.Dependencies.create deps
                |> ResultAsync.wrap (Italian.Query.getServices embassyId)
            | _ ->
                $"Service for '%s{embassy}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
                |> async.Return
        | None ->
            $"Service for '%s{embassyId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return

let getUserService embassyId =
    fun (deps: Services.Dependencies) ->
        match tryParse embassyId with
        | Some embassy' ->
            match embassy' with
            | Embassies.RUS ->
                Russian.Dependencies.create deps
                |> ResultAsync.wrap (Russian.Query.getUserServices embassyId)
            | Embassies.ITA ->
                Italian.Dependencies.create deps
                |> ResultAsync.wrap (Italian.Query.getUserServices embassyId)
            | _ ->
                $"Service for '%s{embassy'}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
                |> async.Return
        | None ->
            $"You have no services for '%s{embassyId.ValueStr}' in your list."
            |> NotFound
            |> Error
            |> async.Return
