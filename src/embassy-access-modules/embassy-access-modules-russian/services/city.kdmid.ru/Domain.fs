module EA.Embassies.Russian.Kdmid.Domain

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain

[<RequireQualifiedAccess>]
module Constants =

    let internal SUPPORTED_SUB_DOMAINS =
        Map
            [ "belgrad", ("Serbia", "Belgrade")
              "budapest", ("Hungary", "Budapest")
              "sarajevo", ("Bosnia", "Sarajevo")
              "berlin", ("Germany", "Berlin")
              "podgorica", ("Montenegro", "Podgorica")
              "tirana", ("Albania", "Tirana")
              "paris", ("France", "Paris")
              "rome", ("Italy", "Rome")
              "dublin", ("Ireland", "Dublin")
              "bern", ("Switzerland", "Bern")
              "helsinki", ("Finland", "Helsinki")
              "hague", ("Netherlands", "Hague")
              "ljubljana", ("Slovenia", "Ljubljana") ]

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
      Embassy: EmbassyGraph
      TimeZone: float
      Confirmation: ConfirmationState }

    member this.CreateRequest serviceName =
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
    { Request: Request
      TimeZone: float }

    static member create timeZone request =
        { Request = request
          TimeZone = timeZone }

type PickOrder =
    { StartOrders: StartOrder list
      notify: Notification -> Async<unit> }

open Web.Http.Domain.Request
open Web.Http.Domain.Response

type Dependencies =
    { updateRequest: EA.Core.Domain.Request.Request -> Async<Result<unit, Error'>>
      getInitialPage: Request -> Web.Http.Domain.Client.Client -> Async<Result<Response<string>, Error'>>
      getCaptcha: Request -> Web.Http.Domain.Client.Client -> Async<Result<Response<byte array>, Error'>>
      solveCaptcha: byte array -> Async<Result<int, Error'>>
      postValidationPage: Request -> RequestContent -> Web.Http.Domain.Client.Client -> Async<Result<string, Error'>>
      postAppointmentsPage: Request -> RequestContent -> Web.Http.Domain.Client.Client -> Async<Result<string, Error'>>
      postConfirmationPage: Request -> RequestContent -> Web.Http.Domain.Client.Client -> Async<Result<string, Error'>> }

    static member create storage ct =
        { updateRequest = fun request -> storage |> EA.Core.DataAccess.Request.Command.update request
          getInitialPage =
            fun request client -> client |> Web.Http.Request.get ct request |> Web.Http.Response.String.read ct
          getCaptcha =
            fun request client -> client |> Web.Http.Request.get ct request |> Web.Http.Response.Bytes.read ct
          solveCaptcha = Web.Captcha.solveToInt ct
          postValidationPage =
            fun request content client ->
                client
                |> Web.Http.Request.post ct request content
                |> Web.Http.Response.String.readContent ct
          postAppointmentsPage =
            fun request content client ->
                client
                |> Web.Http.Request.post ct request content
                |> Web.Http.Response.String.readContent ct
          postConfirmationPage =
            fun request content client ->
                client
                |> Web.Http.Request.post ct request content
                |> Web.Http.Response.String.readContent ct }

type Service =
    { Request: ServiceRequest
      Dependencies: Dependencies }

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

                let! queryParams = uri |> Web.Http.Route.toQueryParams

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
