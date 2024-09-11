[<RequireQualifiedAccess>]
module internal EmbassyAccess.Embassies.Russian.Deps

open Infrastructure
open EmbassyAccess
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

let sendMessage ct =
    { sendRequest = fun request client -> client |> Notification.Repository.Request.send ct request }

let listener ct client =
    match client with
    | Web.Domain.Client.Telegram client ->
        { Listener = Web.Domain.Listener.Telegram(client, Message.tgListener ct) } |> Ok
    | _ -> Error <| NotSupported "EmbassyAccess.Embassies.Russian.Deps.listen"
