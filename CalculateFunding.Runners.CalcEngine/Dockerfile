FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:3.1-alpine AS build

ARG PAT
RUN curl -L https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh  | sh

WORKDIR /src
COPY ["CalculateFunding.Runners.CalcEngine/CalculateFunding.Runners.CalcEngine.csproj", "CalculateFunding.Runners.CalcEngine/"]
COPY ["CalculateFunding.Services.CalcEngine/CalculateFunding.Services.CalcEngine.csproj", "CalculateFunding.Services.CalcEngine/"]
COPY ["CalculateFunding.Services.Calcs/CalculateFunding.Services.Calcs.csproj", "CalculateFunding.Services.Calcs/"]
COPY ["CalculateFunding.Services.Core/CalculateFunding.Services.Core.csproj", "CalculateFunding.Services.Core/"]
COPY ["CalculateFunding.Services.Compiler/CalculateFunding.Services.Compiler.csproj", "CalculateFunding.Services.Compiler/"]
COPY ["CalculateFunding.Services.Processing/CalculateFunding.Services.Processing.csproj", "CalculateFunding.Services.Processing/"]
COPY ["CalculateFunding.Services.CodeMetadataGenerator/CalculateFunding.Services.CodeMetadataGenerator.csproj", "CalculateFunding.Services.CodeMetadataGenerator/"]
COPY ["CalculateFunding.Services.CodeGeneration/CalculateFunding.Services.CodeGeneration.csproj", "CalculateFunding.Services.CodeGeneration/"]
COPY ["CalculateFunding.Services.CodeGeneration.VisualBasic/CalculateFunding.Services.CodeGeneration.VisualBasic.csproj", "CalculateFunding.Services.CodeGeneration.VisualBasic/"]
COPY ["CalculateFunding.Services.CodeGeneration.VisualBasic.Type/CalculateFunding.Services.CodeGeneration.VisualBasic.Type.csproj", "CalculateFunding.Services.CodeGeneration.VisualBasic.Type/"]
COPY ["CalculateFunding.Models.Calcs/CalculateFunding.Models.Calcs.csproj", "CalculateFunding.Models.Calcs/"]
COPY ["CalculateFunding.Models.Publishing/CalculateFunding.Models.Publishing.csproj", "CalculateFunding.Models.Publishing/"]
COPY ["CalculateFunding.Models.Graph/CalculateFunding.Models.Graph.csproj", "CalculateFunding.Models.Graph/"]
COPY ["CalculateFunding.Models.Specs/CalculateFunding.Models.Specs.csproj", "CalculateFunding.Models.Specs/"]
COPY ["CalculateFunding.Models.Code/CalculateFunding.Models.Code.csproj", "CalculateFunding.Models.Code/"]
COPY ["CalculateFunding.Models.Users/CalculateFunding.Models.Users.csproj", "CalculateFunding.Models.Users/"]
COPY ["CalculateFunding.Models.Messages/CalculateFunding.Models.Messages.csproj", "CalculateFunding.Models.Messages/"]
COPY ["CalculateFunding.Models.Providers/CalculateFunding.Models.Providers.csproj", "CalculateFunding.Models.Providers/"]
COPY ["CalculateFunding.Models.Jobs/CalculateFunding.Models.Jobs.csproj", "CalculateFunding.Models.Jobs/"]
COPY ["CalculateFunding.Models.Datasets/CalculateFunding.Models.Datasets.csproj", "CalculateFunding.Models.Datasets/"]
COPY ["CalculateFunding.Models.ProviderLegacy/CalculateFunding.Models.ProviderLegacy.csproj", "CalculateFunding.Models.ProviderLegacy/"]
COPY ["CalculateFunding.Models.Search/CalculateFunding.Models.Search.csproj", "CalculateFunding.Models.Search/"]
COPY ["CalculateFunding.Models.Versioning/CalculateFunding.Models.Versioning.csproj", "CalculateFunding.Models.Versioning/"]
COPY ["CalculateFunding.Repositories.Common.Search/CalculateFunding.Repositories.Common.Search.csproj", "CalculateFunding.Repositories.Common.Search/"]

RUN dotnet nuget add source https://dfe-gov-uk.pkgs.visualstudio.com/_packaging/CalculateFundingService/nuget/v3/index.json -n CalculateFunding -u anything -p "${PAT}" --store-password-in-clear-text

ENV NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED true
ENV VSS_NUGET_EXTERNAL_FEED_ENDPOINTS {\"endpointCredentials\": [{\"endpoint\":\"https://dfe-gov-uk.pkgs.visualstudio.com/_packaging/CalculateFundingService/nuget/v3/index.json\", \"username\":\"ArtifactsDocker\", \"password\":\"${PAT}\"}]}

RUN dotnet restore "CalculateFunding.Runners.CalcEngine/CalculateFunding.Runners.CalcEngine.csproj" -s "https://dfe-gov-uk.pkgs.visualstudio.com/_packaging/CalculateFundingService/nuget/v3/index.json" -s "https://api.nuget.org/v3/index.json"

COPY . .

WORKDIR "/src/CalculateFunding.Runners.CalcEngine"
RUN dotnet build "CalculateFunding.Runners.CalcEngine.csproj" -c Release -o /app --no-restore

FROM build AS publish
RUN dotnet publish "CalculateFunding.Runners.CalcEngine.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CalculateFunding.Runners.CalcEngine.dll"]