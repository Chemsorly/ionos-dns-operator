FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /operator

COPY ./ ./
RUN dotnet publish IonosDnsOperator/IonosDnsOperator.csproj -c Release -o out

# The runner for the application
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /operator
COPY --chown=app:app --from=build /operator/out/ ./

USER app

ENTRYPOINT [ "dotnet", "IonosDnsOperator.dll" ]