[<RequireQualifiedAccess>]
module internal EmbassyAccess.Embassies.Russian.Deps

open EmbassyAccess.Embassies.Russian.Domain

let processRequest ct config storage =
    { Configuration = config
      updateRequest =
        fun request ->

            let operation =
                request
                |> EmbassyAccess.Persistence.Command.Options.Request.Update.Request
                |> EmbassyAccess.Persistence.Command.Request.Update

            storage
            |> EmbassyAccess.Persistence.Repository.Command.Request.execute ct operation
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
