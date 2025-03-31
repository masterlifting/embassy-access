module EA.Russian.Clients.Domain.Kdmid

open System
open System.Threading
open System.Collections.Concurrent
open EA.Core.Domain
open EA.Core.DataAccess
open Infrastructure.Domain
open Web.Clients.Domain

type Client = {
    initHttpClient: string -> Result<Http.Client, Error'>
    updateRequest: Request -> Async<Result<Request, Error'>>
    getCaptcha: Http.Request -> Http.Client -> Async<Result<Http.Response<byte array>, Error'>>
    solveIntCaptcha: byte array -> Async<Result<int, Error'>>
    getInitialPage: Http.Request -> Http.Client -> Async<Result<Http.Response<string>, Error'>>
    postValidationPage: Http.Request -> Http.RequestContent -> Http.Client -> Async<Result<string, Error'>>
    postAppointmentsPage: Http.Request -> Http.RequestContent -> Http.Client -> Async<Result<string, Error'>>
    postConfirmationPage: Http.Request -> Http.RequestContent -> Http.Client -> Async<Result<string, Error'>>
}

type Dependencies = {
    RequestStorage: Request.RequestStorage
    CancellationToken: CancellationToken
}

type Payload = {
    EmbassyId: Graph.NodeId
    Subdomain: string
    Id: int
    Cd: string
    Ems: string option
}

module Constants =
    let internal SUPPORTED_SUB_DOMAINS =
        Map [
            "belgrad", "EMB.RUS.SRB.BEG"
            "budapest", "EMB.RUS.HUN.BUD"
            "sarajevo", "EMB.RUS.BIH.SJJ"
            "berlin", "EMB.RUS.DEU.BER"
            "podgorica", "EMB.RUS.MNE.POD"
            "tirana", "EMB.RUS.ALB.TIA"
            "paris", "EMB.RUS.FRA.PAR"
            "rome", "EMB.RUS.ITA.ROM"
            "dublin", "EMB.RUS.IRL.DUB"
            "bern", "EMB.RUS.CHE.BER"
            "helsinki", "EMB.RUS.FIN.HEL"
            "hague", "EMB.RUS.NLD.HAG"
            "ljubljana", "EMB.RUS.SVN.LJU"
        ]

    module ErrorCode =
        [<Literal>]
        let PAGE_HAS_ERROR = "PageHasError"

        [<Literal>]
        let REQUEST_NOT_CONFIRMED = "RequestNotConfirmed"

        [<Literal>]
        let REQUEST_AWAITING_LIST = "RequestAwaitingList"

        [<Literal>]
        let REQUEST_DELETED = "RequestDeleted"

        [<Literal>]
        let REQUEST_BLOCKED = "RequestBlocked"

        [<Literal>]
        let REQUEST_NOT_FOUND = "RequestNotFound"
