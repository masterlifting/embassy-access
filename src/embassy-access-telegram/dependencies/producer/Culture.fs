[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Producer.Culture

open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services.Domain
open AIProvider.Services.DataAccess
open AIProvider.Services.Dependencies
open AIProvider.Services

type Dependencies =
    { Placeholder: Culture.Placeholder
      translate: Culture.Request -> Async<Result<Culture.Response, Error'>> }

    static member create ct =
        fun (deps: Culture.Dependencies) ->

            let result = ResultBuilder()

            result {

                let translate request =
                    deps |> Culture.Service.translate request ct

                return
                    { Placeholder = Placeholder.create ''' '''
                      translate = translate }
            }
