module EA.Embassies.Russian.Kdmid.Domain

open System
open Infrastructure
open EA.Core.Domain

[<RequireQualifiedAccess>]
module Constants =
    open EA.Core.Domain.Constants

    let internal SUPPORTED_SUB_DOMAINS =
        Map
            [ "belgrad", (Country.SERBIA, City.BELGRADE)
              "budapest", (Country.HUNGARY, City.BUDAPEST)
              "sarajevo", (Country.BOSNIA, City.SARAJEVO)
              "berlin", (Country.GERMANY, City.BERLIN)
              "podgorica", (Country.MONTENEGRO, City.PODGORICA)
              "tirana", (Country.ALBANIA, City.TIRANA)
              "paris", (Country.FRANCE, City.PARIS)
              "rome", (Country.ITALY, City.ROME)
              "dublin", (Country.IRELAND, City.DUBLIN)
              "bern", (Country.SWITZERLAND, City.BERN)
              "helsinki", (Country.FINLAND, City.HELSINKI)
              "hague", (Country.NETHERLANDS, City.HAGUE)
              "ljubljana", (Country.SLOVENIA, City.LJUBLJANA) ]

    module ErrorCode =

        [<Literal>]
        let PAGE_HAS_ERROR = "PageHasError"

        [<Literal>]
        let NOT_CONFIRMED = "NotConfirmed"

        [<Literal>]
        let CONFIRMATION_EXISTS = "ConfirmationExists"

        [<Literal>]
        let REQUEST_DELETED = "RequestDeleted"

type ServiceRequest =
    { Uri: Uri
      Embassy: Embassy
      TimeZone: float
      Confirmation: ConfirmationState }

    member internal this.Create serviceName =
        { Id = RequestId.New
          Service =
            { Name = serviceName
              Payload = this.Uri.ToString()
              Embassy = this.Embassy
              Description = None }
          Attempt = (DateTime.UtcNow, 0)
          ProcessState = Created
          ConfirmationState = this.Confirmation
          Appointments = Set.empty
          Modified = DateTime.UtcNow }


type StartOrder =
    { Request: EA.Core.Domain.Request
      TimeZone: float }

    static member create timeZone request =
        { Request = request
          TimeZone = timeZone }

type PickOrder =
    { StartOrders: StartOrder list
      notify: Notification -> Async<unit> }

type Dependencies =
    { updateRequest: EA.Core.Domain.Request -> Async<Result<EA.Core.Domain.Request, Error'>>
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

    static member create ct storage =
        { updateRequest = fun request -> storage |> EA.Core.Persistence.Repository.Command.Request.update request ct
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


type internal Payload =
    { Country: string
      City: string
      SubDomain: string
      Id: int
      Cd: string
      Ems: string option }

    static member create(uri: Uri) =
        match uri.Host.Split '.' with
        | hostParts when hostParts.Length < 3 -> uri.Host |> NotSupported |> Error
        | hostParts ->
            let payload = ResultBuilder()

            payload {
                let subDomain = hostParts[0]

                let! country, city =
                    match Constants.SUPPORTED_SUB_DOMAINS |> Map.tryFind subDomain with
                    | Some(country, city) -> Ok(country, city)
                    | None -> subDomain |> NotSupported |> Error

                let! queryParams = uri |> Web.Http.Client.Route.toQueryParams

                let! id =
                    queryParams
                    |> Map.tryFind "id"
                    |> Option.map (function
                        | AP.IsInt id when id > 1000 -> id |> Ok
                        | _ -> "id query parameter" |> NotSupported |> Error)
                    |> Option.defaultValue ("id query parameter" |> NotFound |> Error)

                let! cd =
                    queryParams
                    |> Map.tryFind "cd"
                    |> Option.map (function
                        | AP.IsLettersOrNumbers cd -> cd |> Ok
                        | _ -> "cd query parameter" |> NotSupported |> Error)
                    |> Option.defaultValue ("cd query parameter" |> NotFound |> Error)

                let! ems =
                    queryParams
                    |> Map.tryFind "ems"
                    |> Option.map (function
                        | AP.IsLettersOrNumbers ems -> ems |> Some |> Ok
                        | _ -> "ems query parameter" |> NotSupported |> Error)
                    |> Option.defaultValue (None |> Ok)

                return
                    { Country = country
                      City = city
                      SubDomain = subDomain
                      Id = id
                      Cd = cd
                      Ems = ems }
            }
