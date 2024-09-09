[<RequireQualifiedAccess>]
module internal EmbassyAccess.Embassies.Russian.Deps

open EmbassyAccess.Embassies.Russian.Domain
open EmbassyAccess

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

let receiveMessage ct =
    { receiveRequest = fun listener client -> client |> Notification.Repository.Request.receive ct listener }
