[<RequireQualifiedAccess>]
module EA.Embassies.Russian.Kdmid.Dependencies.Order

open System
open Infrastructure.Domain
open Web.Clients
open Web.Clients.Domain.Http

type Dependencies =
    { RestartAttempts: int
      updateRequest: EA.Core.Domain.Request.Request -> Async<Result<EA.Core.Domain.Request.Request, Error'>>
      getCaptcha: Http.Request -> Http.Client -> Async<Result<Http.Response<byte array>, Error'>>
      solveIntCaptcha: byte array -> Async<Result<int, Error'>>
      getInitialPage: Http.Request -> Http.Client -> Async<Result<Http.Response<string>, Error'>>
      postValidationPage: Http.Request -> Http.RequestContent -> Http.Client -> Async<Result<string, Error'>>
      postAppointmentsPage: Http.Request -> Http.RequestContent -> Http.Client -> Async<Result<string, Error'>>
      postConfirmationPage: Http.Request -> Http.RequestContent -> Http.Client -> Async<Result<string, Error'>> }

    static member create requestStorage ct =
        { RestartAttempts = 3
          updateRequest = fun request -> requestStorage |> EA.Core.DataAccess.Request.Command.update request
          getInitialPage =
            fun request client -> client |> Http.Request.get request ct |> Http.Response.String.read ct
          getCaptcha =
            fun request client -> client |> Http.Request.get request ct |> Http.Response.Bytes.read ct
          solveIntCaptcha = Web.Captcha.solveToInt ct
          postValidationPage =
            fun request content client ->
                client
                |> Http.Request.post request content ct
                |> Http.Response.String.readContent ct
          postAppointmentsPage =
            fun request content client ->
                client
                |> Http.Request.post request content ct
                |> Http.Response.String.readContent ct
          postConfirmationPage =
            fun request content client ->
                client
                |> Http.Request.post request content ct
                |> Http.Response.String.readContent ct }
