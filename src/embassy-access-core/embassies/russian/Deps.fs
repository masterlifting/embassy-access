[<RequireQualifiedAccess>]
module internal EmbassyAccess.Embassies.Russian.Deps

open Infrastructure
open EmbassyAccess.Embassies.Russian.Domain

let processRequest ct config storage =
    { Configuration = config
      updateRequest =
        fun request ->
            storage
            |> EmbassyAccess.Persistence.Repository.Command.Request.update ct request
      getInitialPage =
        fun request client ->
            client
            |> Web.Http.Client.Request.get ct request
            |> Web.Http.Client.Response.String.read ct
      getCaptcha =
        fun request client ->
            client
            |> Web.Http.Client.Request.get ct request
            |> Web.Http.Client.Response.Bytes.read ct
      solveCaptcha = Web.Captcha.solveToInt ct
      postValidationPage =
        fun request content client ->
            client
            |> Web.Http.Client.Request.post ct request content
            |> Web.Http.Client.Response.String.readContent ct
      postAppointmentsPage =
        fun request content client ->
            client
            |> Web.Http.Client.Request.post ct request content
            |> Web.Http.Client.Response.String.readContent ct
      postConfirmationPage =
        fun request content client ->
            client
            |> Web.Http.Client.Request.post ct request content
            |> Web.Http.Client.Response.String.readContent ct }

let sendMessage ct message = message |> Telegram.send ct

let createListener ct context =
    match context with
    | Web.Domain.Telegram token ->
        Web.Telegram.Client.create token
        |> Result.map (fun client -> Web.Domain.Listener.Telegram(client, Telegram.receive ct))
    | _ ->
        Error
        <| NotSupported $"Context '{context}'. EmbassyAccess.Embassies.Russian.Deps.createListener"
