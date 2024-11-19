module EA.Embassies.Russian.Kdmid.Domain

open System
open Infrastructure
open EA.Core.Domain

[<RequireQualifiedAccess>]
module Constants =
    let internal SUPPORTED__SUB_DOMAINS =
        Map
            [ "belgrad", Belgrade |> Serbia
              "budapest", Budapest |> Hungary
              "sarajevo", Sarajevo |> Bosnia
              "berlin", Berlin |> Germany
              "podgorica", Podgorica |> Montenegro
              "tirana", Tirana |> Albania
              "paris", Paris |> France
              "rome", Rome |> Italy
              "dublin", Dublin |> Ireland
              "bern", Bern |> Switzerland
              "helsinki", Helsinki |> Finland
              "hague", Hague |> Netherlands
              "ljubljana", Ljubljana |> Slovenia ]

    module ErrorCodes =

        [<Literal>]
        let PAGE_HAS_ERROR = "PageHasError"

        [<Literal>]
        let NOT_CONFIRMED = "NotConfirmed"

        [<Literal>]
        let CONFIRMATION_EXISTS = "ConfirmationExists"

        [<Literal>]
        let REQUEST_DELETED = "RequestDeleted"

type Request =
    { Uri: Uri
      Country: Country
      TimeZone: float
      Confirmation: ConfirmationState }

    member internal this.Create serviceName =
        { Id = RequestId.New
          Service =
            { Name = serviceName
              Payload = this.Uri.ToString()
              Embassy = Russian this.Country
              Description = None }
          Attempt = (DateTime.UtcNow, 0)
          ProcessState = Created
          ConfirmationState = this.Confirmation
          Appointments = Set.empty
          Modified = DateTime.UtcNow }

type Dependencies =
    { updateRequest: EA.Core.Domain.Request -> Async<Result<EA.Core.Domain.Request, Error'>>
      createHttpClient: string -> Result<Web.Http.Domain.Client, Error'>
      getInitialPage:
          Web.Http.Domain.Request -> Web.Http.Domain.Client -> Async<Result<Web.Http.Domain.Response<string>, Error'>>
      getCaptcha:
          Web.Http.Domain.Request
              -> Web.Http.Domain.Client
              -> Async<Result<Web.Http.Domain.Response<byte array>, Error'>>
      solveCaptcha: byte array -> Async<Result<int, Error'>>
      postValidationPage:
          Web.Http.Domain.Request
              -> Web.Http.Domain.RequestContent
              -> Web.Http.Domain.Client
              -> Async<Result<string, Error'>>
      postAppointmentsPage:
          Web.Http.Domain.Request
              -> Web.Http.Domain.RequestContent
              -> Web.Http.Domain.Client
              -> Async<Result<string, Error'>>
      postConfirmationPage:
          Web.Http.Domain.Request
              -> Web.Http.Domain.RequestContent
              -> Web.Http.Domain.Client
              -> Async<Result<string, Error'>> }

type internal Payload =
    { Country: Country
      SubDomain: string
      Id: int
      Cd: string
      Ems: string option }
