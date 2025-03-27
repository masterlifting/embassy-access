[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Culture

open Infrastructure.Prelude
open Infrastructure.Domain
open AIProvider.Services.Domain

type Dependencies =
    { Shield: Culture.Shield
      translate: Request -> Async<Result<Response, Error'>> }

    static member create ct =
        fun (persistence: Persistence.Dependencies) (aiProvider: AIProvider.Dependencies) ->
            let result = ResultBuilder()

            result {
                let! aiProvider = aiProvider.initProvider ()
                let! cultureStorage = persistence.initCultureStorage ()

                let cultureDeps: AIProvider.Services.Dependencies.Culture.Dependencies =
                    { Provider = aiProvider
                      Storage = cultureStorage }

                let translate request =
                    cultureDeps |> AIProvider.Services.Culture.translate request ct

                return
                    { Shield = Shield.create ''' '''
                      translate = translate }
            }
