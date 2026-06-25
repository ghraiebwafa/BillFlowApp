# Build a BillFlow service. Override PROJECT_PATH and ASSEMBLY_NAME per service.
# Example:
#   docker build --build-arg PROJECT_PATH=Services/BillFlow.AuthService/BillFlow.AuthService.csproj --build-arg ASSEMBLY_NAME=BillFlow.AuthService -t billflow-auth .

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG PROJECT_PATH
WORKDIR /src

COPY BillFlow.sln ./
COPY DataAccess/ DataAccess/
COPY Services/ Services/

RUN dotnet restore "${PROJECT_PATH}"
RUN dotnet publish "${PROJECT_PATH}" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
ARG ASSEMBLY_NAME
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ARG ASSEMBLY_NAME
ENV ASSEMBLY_NAME=${ASSEMBLY_NAME}

ENTRYPOINT ["sh", "-c", "exec dotnet ${ASSEMBLY_NAME}.dll"]
