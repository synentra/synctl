<div align="center">

# vectractl

**The official CLI for [Vectra](https://github.com/cortexiumlabs/vectra) — Intent-Aware Governance Gateway for Autonomous AI Agents**

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![GitHub release](https://img.shields.io/github/v/release/cortexiumlabs/vectractl)](https://github.com/cortexiumlabs/vectractl/releases)

`vectractl` gives you full command-line control over the Vectra gateway: install, run, and update the gateway engine; register and manage AI agents; define governance policies; review and resolve Human-in-the-Loop (HITL) approvals; and exchange agent credentials for JWT bearer tokens — all from your terminal.

</div>

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Commands](#commands)
  - [init](#init)
  - [run](#run)
  - [stop](#stop)
  - [update](#update)
  - [uninstall](#uninstall)
  - [agents](#agents)
  - [policies](#policies)
  - [hitl](#hitl)
  - [token](#token)
- [Global Options](#global-options)
- [Configuration](#configuration)
- [Project Structure](#project-structure)
- [Building from Source](#building-from-source)
- [Contributing](#contributing)
- [License](#license)

## Overview

Vectra is an **Intent-Aware Governance Gateway** that sits between autonomous AI agents and the outside world. It enforces governance policies, routes actions through Human-in-the-Loop review when required, and provides a full audit trail.

`vectractl` is the companion CLI that lets platform engineers, DevOps teams, and AI developers interact with the gateway from the terminal or from CI/CD pipelines.

```
AI Agent  ──►  Vectra Gateway  ──►  External Systems
                    │
               vectractl
          (manage, audit, approve)
```

## Prerequisites

| Requirement | Minimum Version |
|---|---|
| [.NET Runtime](https://dotnet.microsoft.com/download) | 10.0 |
| [Docker](https://www.docker.com/get-started) *(optional)* | 24.x |

Docker is only required when using the `--docker` deployment mode.

## Installation

### Option 1 – Download a pre-built binary (recommended)

Pre-built binaries for Windows, macOS, and Linux are available on the [Releases](https://github.com/cortexiumlabs/vectractl/releases) page.

```bash
# Example: Linux / macOS
curl -sSL https://github.com/cortexiumlabs/vectractl/releases/latest/download/vectractl-linux-x64.tar.gz | tar xz
sudo mv vectractl /usr/local/bin/
```

### Option 2 – Build from source

```bash
git clone https://github.com/cortexiumlabs/vectractl.git
cd vectractl
dotnet publish src/VectraCtl/VectraCtl.csproj -c Release -o ./publish
# Add ./publish to your PATH
```

## Quick Start

```bash
# 1. Install and start the Vectra gateway (local binary)
vectractl init
vectractl run

# --- or, using Docker ---
vectractl init --docker --port 7080 --mount /data/vectra
vectractl run  --docker

# 2. Register an AI agent
vectractl agents register --name "my-agent" --owner "platform-team" --secret "s3cr3t"

# 3. Assign a governance policy to the agent
vectractl agents assign-policy --agent-id <GUID> --policy "strict-pii"

# 4. Obtain a JWT bearer token for the agent
vectractl token --agent-id <GUID> --secret "s3cr3t"

# 5. Review pending Human-in-the-Loop approval requests
vectractl hitl list
vectractl hitl approve --id <REQUEST-ID> --comment "Reviewed and approved"
```

## Commands

### `init`

Downloads and installs the Vectra gateway engine locally or pulls the Docker image.

```
vectractl init [OPTIONS]
```

| Option | Description | Default |
|---|---|---|
| `--docker` | Pull and configure the Docker image instead of a local binary | `false` |
| `--version <ver>` | Specific gateway version to install (e.g. `1.2.3`) | latest |
| `--container-name <name>` | Docker container name | `vectra-gateway` |
| `--port <port>` | Host port mapped to the container | `7080` |
| `--mount <path>` | Host path to mount as the container data directory | — |
| `--container-path <path>` | Container-internal data path | `/app/data` |

**Examples**

```bash
# Install the latest binary release
vectractl init

# Install a specific version
vectractl init --version 1.5.0

# Pull and configure a Docker deployment
vectractl init --docker --port 7080 --mount ~/vectra-data
```

### `run`

Starts the Vectra gateway (binary or Docker container).

```
vectractl run [OPTIONS]
```

| Option | Description | Default |
|---|---|---|
| `--docker` | Start the Docker-based gateway | `false` |
| `--background` | Run detached (no log streaming) | `false` |

**Examples**

```bash
vectractl run                        # start local binary (foreground)
vectractl run --background           # start local binary (background)
vectractl run --docker               # start Docker container (foreground)
vectractl run --docker --background  # start Docker container (detached)
```

### `stop`

Stops the running Vectra gateway.

```
vectractl stop [OPTIONS]
```

| Option | Description |
|---|---|
| `--docker` | Stop the Docker container instead of the local binary process |

```bash
vectractl stop
vectractl stop --docker
```

### `update`

Updates the Vectra gateway binary to a newer (or specific) version.

```
vectractl update [OPTIONS]
```

| Option | Description | Default |
|---|---|---|
| `--version <ver>` | Target version to install | latest |
| `--force` | Stop the running gateway automatically before updating | `false` |

```bash
vectractl update                    # update to latest
vectractl update --version 2.0.0   # update to a specific version
vectractl update --force            # stop gateway automatically, then update
```

### `uninstall`

Removes the Vectra gateway engine (binary or Docker).

```
vectractl uninstall [OPTIONS]
```

| Option | Description |
|---|---|
| `--docker` | Remove the Docker-based deployment |
| `--force` | Force removal even when the engine is still running |
| `--remove-data` | Also delete the engine data directory *(Docker mode only)* |

```bash
vectractl uninstall
vectractl uninstall --docker --remove-data
vectractl uninstall --force
```

### `agents`

Manage AI agents registered in the Vectra gateway.

#### `agents list`

```bash
vectractl agents list [--page <n>] [--page-size <n>] [-o json|table]
```

#### `agents register`

```bash
vectractl agents register --name <name> --owner <owner> --secret <secret>
```

| Option | Description | Required |
|---|---|---|
| `--name` | Human-readable agent name | ✅ |
| `--owner` | Owner / team identifier | ✅ |
| `--secret` | Client secret for the agent | ✅ |

#### `agents assign-policy`

```bash
vectractl agents assign-policy --agent-id <GUID> --policy <policy-name>
```

| Option | Description | Required |
|---|---|---|
| `--agent-id` | Agent ID (GUID) | ✅ |
| `--policy` | Policy name to assign | ✅ |

#### `agents delete`

```bash
vectractl agents delete --agent-id <GUID>
```

### `policies`

Browse governance policies defined in the Vectra gateway.

#### `policies list`

```bash
vectractl policies list [--page <n>] [--page-size <n>] [-o json|table]
```

#### `policies details`

```bash
vectractl policies details --name <policy-name> [-o json|table]
```

### `hitl`

Manage Human-in-the-Loop review requests generated by the gateway when an agent action requires human approval.

#### `hitl list`

```bash
vectractl hitl list [--page <n>] [--page-size <n>] [-o json|table]
```

#### `hitl status`

```bash
vectractl hitl status --id <request-id> [-o json|table]
```

#### `hitl approve`

```bash
vectractl hitl approve --id <request-id> [--comment "Approved"]
```

#### `hitl deny`

```bash
vectractl hitl deny --id <request-id> [--comment "Rejected: policy violation"]
```

### `token`

Exchange agent credentials for a short-lived JWT bearer token that can be used to authenticate API calls to the Vectra gateway.

```
vectractl token --agent-id <GUID> --secret <secret> [-o json|table]
```

| Option | Description | Required |
|---|---|---|
| `--agent-id` | Agent ID (GUID) | ✅ |
| `--secret` | Agent client secret | ✅ |
| `-o, --output` | Output format (`json` \| `table`) | — |

```bash
vectractl token --agent-id 3fa85f64-5717-4562-b3fc-2c963f66afa6 --secret "s3cr3t"
```

## Global Options

These options are available on all list-style subcommands:

| Option | Description | Default |
|---|---|---|
| `--page <n>` | Page number for paginated results | `1` |
| `--page-size <n>` | Number of results per page | `25` |
| `-o, --output` | Output format: `json` or `table` | `json` |

## Configuration

`vectractl` stores its runtime configuration in `~/.vectra/appsettings.json`. This file is created automatically when you run `vectractl init` and is updated by subsequent commands.

```jsonc
{
  "DeploymentMode": "Binary",   // "Binary" or "Docker"
  "Binary": {
    "Version": "1.5.0"
  },
  "Docker": {
    "ImageName": "cortexiumlabs/vectra",
    "Tag": "1.5.0",
    "ContainerName": "vectra-gateway",
    "Port": 7080,
    "HostDataPath": "/home/user/vectra-data",
    "ContainerDataPath": "/app/data"
  }
}
```

You can edit this file manually or use `vectractl init` to regenerate it.

## Project Structure

```
vectractl/
├── src/
│   ├── VectraCtl/                   # CLI entry point & command definitions
│   │   ├── Commands/                # One file per top-level command
│   │   ├── ApplicationBuilders/     # CLI application wiring (DI, System.CommandLine)
│   │   ├── Services/                # Location, versioning, Spectre.Console logger
│   │   └── Program.cs
│   │
│   ├── VectraCtl.Core/              # Abstractions & domain models (no external deps)
│   │   ├── Models/                  # AppSettings, DockerRunOptions, etc.
│   │   ├── Services/                # Service interfaces (Docker, GitHub, Config, …)
│   │   └── Serialization/           # JSON serializer interfaces
│   │
│   └── VectraCtl.Infrastructure/    # Concrete implementations
│       ├── Services/                # Docker, GitHub release manager, process handler
│       └── Serialization/           # System.Text.Json implementation
└── README.md
```

## Building from Source

```bash
# Restore dependencies
dotnet restore

# Build (Debug)
dotnet build

# Build self-contained binary for your platform
dotnet publish src/VectraCtl/VectraCtl.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o ./publish

# Run tests (when available)
dotnet test
```

Supported runtime identifiers: `win-x64`, `linux-x64`, `osx-x64`, `osx-arm64`.

## Contributing

Contributions are welcome! Please follow these steps:

1. **Fork** the repository and create a feature branch (`git checkout -b feature/my-feature`).
2. Make your changes and ensure the project builds cleanly (`dotnet build`).
3. **Commit** with a clear, descriptive message.
4. Open a **Pull Request** against `main` with a description of what was changed and why.

Please keep PRs focused on a single concern. For large changes, open an issue first to discuss the approach.

## License

© Cortexium Labs. Licensed under the [Apache License, Version 2.0](LICENSE).
