# agent-atlas

An AI-assisted development project using GitHub Copilot with Squad agent teams and .NET Aspire MCP integration.

## Prerequisites

- [Node.js](https://nodejs.org/) (for Squad)
- [.NET SDK 10+](https://dotnet.microsoft.com/download)
- [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview) (`dotnet tool install --global aspire.cli`)

## Setup

### 1. Install Squad

Squad provides an AI development team through GitHub Copilot. It has already been installed in this repository via:

```bash
npx github:bradygaster/squad
```

This creates agent configurations, GitHub Actions workflows, and templates under `.github/agents/`, `.squad/`, and `.squad-templates/`.

### 2. Install the Aspire CLI

The Aspire CLI provides the `aspire agent mcp` command used by the MCP server integration:

```bash
dotnet tool install --global aspire.cli
```

> **Note:** The legacy `dotnet workload install aspire` is deprecated as of .NET 10. Aspire is now available as NuGet packages and via the Aspire CLI tool above.

### 3. Aspire MCP Integration

The Aspire MCP (Model Context Protocol) server allows GitHub Copilot to interact with your running Aspire applications. Configuration is already set up in this repository:

- **GitHub Copilot CLI / Copilot Chat**: `.copilot/mcp-config.json` — team-shared, committed to the repo
- **VS Code**: `.vscode/mcp.json` — workspace-level configuration

To initialize MCP configuration for your local environment, run:

```bash
aspire mcp init
```

Or manually configure your global `~/.copilot/mcp-config.json`:

```json
{
  "mcpServers": {
    "aspire": {
      "type": "local",
      "command": "aspire",
      "args": ["agent", "mcp"],
      "env": {
        "DOTNET_ROOT": "${DOTNET_ROOT}"
      },
      "tools": ["*"]
    }
  }
}
```

### 4. Start Using Squad

1. Open GitHub Copilot in your terminal or VS Code
2. Type `/agent` (CLI) or `/agents` (VS Code) and select **Squad**
3. Describe what you're building
