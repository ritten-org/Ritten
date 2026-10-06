[![Ritten](https://github.com/ritten-org/Ritten/actions/workflows/ritten.yml/badge.svg)](https://github.com/ritten-org/Ritten/actions/workflows/ritten.yml)

Your build, ritten in C#.

## Installation

Ritten is a .NET tool. Pin it per repository so that everyone — and CI — runs the same version:

```sh
dotnet new tool-manifest
dotnet tool install ritten
```

## Usage

Describe the project in a `ritten.json` at its root:

```json
{
    "build": {
        "project": "src/Thing/Thing.csproj"
    }
}
```

Then run a job from anywhere in the repository:

```sh
dotnet ritten build    # compile and test
dotnet ritten install  # build, pack, and install the tool globally from the working tree
dotnet ritten check    # build, plus release checks: formatting, version, and changelog
dotnet ritten deploy   # check, then pack, tag, release, and publish
```

### Tracing

Enable OpenTelemetry by setting an OTLP endpoint. The standard `OTEL_*` variables apply:

```sh
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 OTEL_SERVICE_NAME=thing dotnet ritten build
```

A host built on Ritten can opt in with `builder.AddOpenTelemetry(otel => …)` and a reference to 
`Ritten.OpenTelemetry` package.

## License

See [LICENSE](LICENSE).
