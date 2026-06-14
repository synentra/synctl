<div align="center">

# SynCtl

**The official CLI for [Synentra](https://github.com/synentra/synctl) — Intent-Aware Governance Gateway for Autonomous AI Agents**

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![GitHub release](https://img.shields.io/github/v/release/synentra/synctl)](https://github.com/synentra/synctl/releases)

`synctl` gives you full command-line control over the Synentra gateway: install, run, and update the gateway engine; register and manage AI agents; define governance policies; review and resolve Human-in-the-Loop (HITL) approvals; and exchange agent credentials for JWT bearer tokens — all from your terminal.

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

Synentra is an **Intent-Aware Governance Gateway** that sits between autonomous AI agents and the outside world. It enforces governance policies, routes actions through Human-in-the-Loop review when required, and provides a full audit trail.

`synctl` is the companion CLI that lets platform engineers, DevOps teams, and AI developers interact with the gateway from the terminal or from CI/CD pipelines.

## Prerequisites

| Requirement | Minimum Version |
|---|---|
| [.NET Runtime](https://dotnet.microsoft.com/download) | 10.0 |
| [Docker](https://www.docker.com/get-started) *(optional)* | 24.x |

Docker is only required when using the `--docker` deployment mode.

## Installation

### Option 1 – Download a pre-built binary (recommended)

Pre-built binaries for Windows, macOS, and Linux are available on the [Releases](https://github.com/synentra/synctl/releases) page.

```bash
# Example: Linux / macOS
curl -sSL https://github.com/synentra/synctl/releases/latest/download/synctl-linux-x64.tar.gz | tar xz
sudo mv synctl /usr/local/bin/
```

### Option 2 – Build from source

```bash
git clone https://github.com/synentra/synctl.git
cd synctl
dotnet publish src/SynCtl/SynCtl.csproj -c Release -o ./publish
# Add ./publish to your PATH
```

## Quick Start

```bash
# 1. Install and start the Synentra gateway (local binary)
synctl init
synctl run

# --- or, using Docker ---
synctl init --docker --port 7080 --mount /data/synentra
synctl run  --docker

# 2. Register an AI agent
synctl agents register --name "my-agent" --owner "platform-team" --secret "s3cr3t"

# 3. Assign a governance policy to the agent
synctl agents assign-policy --agent-id <GUID> --policy "strict-pii"

# 4. Obtain a JWT bearer token for the agent
synctl token --agent-id <GUID> --secret "s3cr3t"

# 5. Review pending Human-in-the-Loop approval requests
synctl hitl list
synctl hitl approve --id <REQUEST-ID> --comment "Reviewed and approved"
```

## Commands

### `init`

Downloads and installs the Synentra gateway engine locally or pulls the Docker image.

```
synctl init [OPTIONS]
```

| Option | Description | Default |
|---|---|---|
| `--docker` | Pull and configure the Docker image instead of a local binary | `false` |
| `--version <ver>` | Specific gateway version to install (e.g. `1.2.3`) | latest |
| `--container-name <name>` | Docker container name | `synentra-gateway` |
| `--port <port>` | Host port mapped to the container | `7080` |
| `--mount <path>` | Host path to mount as the container data directory | — |
| `--container-path <path>` | Container-internal data path | `/app/data` |

**Examples**

```bash
# Install the latest binary release
synctl init

# Install a specific version
synctl init --version 1.5.0

# Pull and configure a Docker deployment
synctl init --docker --port 7080 --mount ~/synentra-data
```

### `run`

Starts the Synentra gateway (binary or Docker container).

```
synctl run [OPTIONS]
```

| Option | Description | Default |
|---|---|---|
| `--docker` | Start the Docker-based gateway | `false` |
| `--background` | Run detached (no log streaming) | `false` |

**Examples**

```bash
synctl run                        # start local binary (foreground)
synctl run --background           # start local binary (background)
synctl run --docker               # start Docker container (foreground)
synctl run --docker --background  # start Docker container (detached)
```

### `stop`

Stops the running Synentra gateway.

```
synctl stop [OPTIONS]
```

| Option | Description |
|---|---|
| `--docker` | Stop the Docker container instead of the local binary process |

```bash
synctl stop
synctl stop --docker
```

### `update`

Updates the Synentra gateway binary to a newer (or specific) version.

```
synctl update [OPTIONS]
```

| Option | Description | Default |
|---|---|---|
| `--version <ver>` | Target version to install | latest |
| `--force` | Stop the running gateway automatically before updating | `false` |

```bash
synctl update                    # update to latest
synctl update --version 2.0.0   # update to a specific version
synctl update --force            # stop gateway automatically, then update
```

### `uninstall`

Removes the Synentra gateway engine (binary or Docker).

```
synctl uninstall [OPTIONS]
```

| Option | Description |
|---|---|
| `--docker` | Remove the Docker-based deployment |
| `--force` | Force removal even when the engine is still running |
| `--remove-data` | Also delete the engine data directory *(Docker mode only)* |

```bash
synctl uninstall
synctl uninstall --docker --remove-data
synctl uninstall --force
```

### `agents`

Manage AI agents registered in the Synentra gateway.

#### `agents list`

```bash
synctl agents list [--page <n>] [--page-size <n>] [-o json|table]
```

#### `agents register`

```bash
synctl agents register --name <name> --owner <owner> --secret <secret>
```

| Option | Description | Required |
|---|---|---|
| `--name` | Human-readable agent name | ✅ |
| `--owner` | Owner / team identifier | ✅ |
| `--secret` | Client secret for the agent | ✅ |

#### `agents assign-policy`

```bash
synctl agents assign-policy --agent-id <GUID> --policy <policy-name>
```

| Option | Description | Required |
|---|---|---|
| `--agent-id` | Agent ID (GUID) | ✅ |
| `--policy` | Policy name to assign | ✅ |

#### `agents delete`

```bash
synctl agents delete --agent-id <GUID>
```

### `policies`

Browse governance policies defined in the Synentra gateway.

#### `policies list`

```bash
synctl policies list [--page <n>] [--page-size <n>] [-o json|table]
```

#### `policies details`

```bash
synctl policies details --name <policy-name> [-o json|table]
```

### `hitl`

Manage Human-in-the-Loop review requests generated by the gateway when an agent action requires human approval.

#### `hitl list`

```bash
synctl hitl list [--page <n>] [--page-size <n>] [-o json|table]
```

#### `hitl status`

```bash
synctl hitl status --id <request-id> [-o json|table]
```

#### `hitl approve`

```bash
synctl hitl approve --id <request-id> [--comment "Approved"]
```

#### `hitl deny`

```bash
synctl hitl deny --id <request-id> [--comment "Rejected: policy violation"]
```

### `token`

Exchange agent credentials for a short-lived JWT bearer token that can be used to authenticate API calls to the Synentra gateway.

```
synctl token --agent-id <GUID> --secret <secret> [-o json|table]
```

| Option | Description | Required |
|---|---|---|
| `--agent-id` | Agent ID (GUID) | ✅ |
| `--secret` | Agent client secret | ✅ |
| `-o, --output` | Output format (`json` \| `table`) | — |

```bash
synctl       token --agent-id 3fa85f64-5717-4562-b3fc-2c963f66afa6 --secret "s3cr3t"
```

## Global Options

These options are available on all list-style subcommands:

| Option | Description | Default |
|---|---|---|
| `--page <n>` | Page number for paginated results | `1` |
| `--page-size <n>` | Number of results per page | `25` |
| `-o, --output` | Output format: `json` or `table` | `json` |

## Configuration

`synctl` stores its runtime configuration in `~/.synentra/appsettings.json`. This file is created automatically when you run `synctl init` and is updated by subsequent commands.

```jsonc
{
  "DeploymentMode": "Binary",   // "Binary" or "Docker"
  "Binary": {
    "Version": "1.5.0"
  },
  "Docker": {
    "ImageName": "synentra/synentra",
    "Tag": "1.5.0",
    "ContainerName": "synentra-gateway",
    "Port": 7080,
    "HostDataPath": "/home/user/synentra-data",
    "ContainerDataPath": "/app/data"
  }
}
```

You can edit this file manually or use `synctl init` to regenerate it.

## Project Structure

```
synctl/
├── src/
│   ├── SynentraCtl/                   # CLI entry point & command definitions
│   │   ├── Commands/                # One file per top-level command
│   │   ├── ApplicationBuilders/     # CLI application wiring (DI, System.CommandLine)
│   │   ├── Services/                # Location, versioning, Spectre.Console logger
│   │   └── Program.cs
│   │
│   ├── SynentraCtl.Core/              # Abstractions & domain models (no external deps)
│   │   ├── Models/                  # AppSettings, DockerRunOptions, etc.
│   │   ├── Services/                # Service interfaces (Docker, GitHub, Config, …)
│   │   └── Serialization/           # JSON serializer interfaces
│   │
│   └── SynentraCtl.Infrastructure/    # Concrete implementations
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
dotnet publish src/SynentraCtl/SynentraCtl.csproj \
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

© Synentra. Licensed under the [Apache License, Version 2.0](LICENSE).
