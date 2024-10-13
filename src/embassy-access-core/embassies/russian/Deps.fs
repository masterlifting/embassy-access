[<RequireQualifiedAccess>]
module internal EA.Embassies.Russian.Deps

open EA.Embassies.Russian.Domain

let processRequest ct config storage =
    { Configuration = config
      updateRequest =
        fun request ->

            let operation =
                request
                |> EA.Persistence.Command.Options.Request.Update.Request
                |> EA.Persistence.Command.Request.Update

            storage |> EA.Persistence.Repository.Command.Request.execute ct operation
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
