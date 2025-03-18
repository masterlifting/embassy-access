[<RequireQualifiedAccess>]
module EA.Telegram.Services.Culture.Payloads.ButtonsGroup

open Infrastructure.Prelude
open AIProvider.Services.Domain.Culture
open Web.Telegram.Domain.Producer
open EA.Telegram.Dependencies.Consumer

let translate culture (payload: Payload<ButtonsGroup>) =
    fun (deps: Culture.Dependencies) ->
        let group = payload.Value

        let items =
            { Id = group.Name; Value = group.Name }
            :: (group.Buttons
                |> Set.map (fun button ->
                    { Id = button.Callback.Value
                      Value = button.Name })
                |> Set.toList)

        let request = { Culture = culture; Items = items }

        deps.translate request
        |> ResultAsync.map (fun response ->
            let responseItemsMap =
                response.Items
                |> List.map (fun item -> item.Id, item.Result |> Option.defaultValue item.Value)
                |> Map.ofList

            let buttonsGroupName =
                responseItemsMap |> Map.tryFind group.Name |> Option.defaultValue group.Name

            let buttons =
                group.Buttons
                |> Set.map (fun button ->
                    let buttonName =
                        responseItemsMap
                        |> Map.tryFind button.Callback.Value
                        |> Option.defaultValue button.Name

                    { button with Name = buttonName })

            { group with
                Name = buttonsGroupName
                Buttons = buttons })
        |> ResultAsync.map (fun value -> { payload with Value = value } |> ButtonsGroup)
