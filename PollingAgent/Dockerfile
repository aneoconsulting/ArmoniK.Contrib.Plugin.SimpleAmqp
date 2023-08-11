ARG IMAGE

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS adapters
WORKDIR /src
COPY . . 
WORKDIR "/src/ArmoniK.Contrib.Plugin.SimpleAmqp/src"
RUN dotnet publish "ArmoniK.Contrib.Plugin.SimpleAmqp.csproj" -c Release -o /app/publish/plugin /p:UseAppHost=false


FROM $IMAGE AS final
COPY --from=adapters /app/publish/plugin /adapters/queue/simpleamqp