[<RequireQualifiedAccess>]
module EA.Embassies.Russian.Kdmid.Domain.Constants

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
