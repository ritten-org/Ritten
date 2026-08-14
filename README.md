[![Ritten](https://github.com/ritten-org/Ritten/actions/workflows/ritten.yml/badge.svg)](https://github.com/ritten-org/Ritten/actions/workflows/ritten.yml)

Your build, ritten in C#.

## Installation

Ritten is a .NET tool. Pin it per repository so that everyone — and CI — runs the same version:

```sh
dotnet new tool-manifest
dotnet tool install Ritten
```

## Usage

Describe the project in a `ritten.json` at its root:

```json
{
    "build": {
        "project": "src/Thing/Thing.csproj"
    },
    "changelog": {
        "repository": "https://github.com/you/thing"
    }
}
```

Then run a job from anywhere in the repository:

```sh
dotnet ritten verify   # compile and test
dotnet ritten build    # verify, plus version and changelog validation
dotnet ritten deploy   # build, then pack, tag, release, and publish
```

## License

See [LICENSE](LICENSE).
