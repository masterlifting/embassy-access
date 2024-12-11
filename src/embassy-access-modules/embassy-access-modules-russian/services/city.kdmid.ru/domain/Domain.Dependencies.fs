[<AutoOpen>]
module EA.Embassies.Russian.Kdmid.Domain.Dependencies

open System
open Infrastructure.Domain
open Web.Http.Domain.Client
open Web.Http.Domain.Request
open Web.Http.Domain.Response

type Dependencies =
    { updateRequest: EA.Core.Domain.Request.Request -> Async<Result<EA.Core.Domain.Request.Request, Error'>>
      getInitialPage: Request -> HttpClient -> Async<Result<Response<string>, Error'>>
      getCaptcha: Request -> HttpClient -> Async<Result<Response<byte array>, Error'>>
      solveCaptcha: byte array -> Async<Result<int, Error'>>
      postValidationPage: Request -> RequestContent -> HttpClient -> Async<Result<string, Error'>>
      postAppointmentsPage: Request -> RequestContent -> HttpClient -> Async<Result<string, Error'>>
      postConfirmationPage: Request -> RequestContent -> HttpClient -> Async<Result<string, Error'>> }

    static member create storage ct =
        { updateRequest = fun request -> storage |> EA.Core.DataAccess.Request.Command.update request
          getInitialPage =
            fun request client -> client |> Web.Http.Request.get request ct |> Web.Http.Response.String.read ct
          getCaptcha =
            fun request client -> client |> Web.Http.Request.get request ct |> Web.Http.Response.Bytes.read ct
          solveCaptcha = Web.Captcha.solveToInt ct
          postValidationPage =
            fun request content client ->
                client
                |> Web.Http.Request.post request content ct
                |> Web.Http.Response.String.readContent ct
          postAppointmentsPage =
            fun request content client ->
                client
                |> Web.Http.Request.post request content ct
                |> Web.Http.Response.String.readContent ct
          postConfirmationPage =
            fun request content client ->
                client
                |> Web.Http.Request.post request content ct
                |> Web.Http.Response.String.readContent ct }
