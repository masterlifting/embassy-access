module EA.Embassies.Russian.Kdmid.Deps

open EA.Persistence
open EA.Embassies.Russian.Kdmid.Domain

let processRequest ct config storage =
    { Configuration = config
      storageUpdateRequest = fun request -> storage |> Repository.Command.Request.update request ct
      httpStringGet =
        fun request client ->
            client
            |> Web.Http.Client.Request.get ct request
            |> Web.Http.Client.Response.String.read ct
      httpBytesGet =
        fun request client ->
            client
            |> Web.Http.Client.Request.get ct request
            |> Web.Http.Client.Response.Bytes.read ct
      solveCaptcha = Web.Captcha.solveToInt ct
      httpStringPost =
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
