import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Check, Copy, ExternalLink } from "lucide-react"

type Platform = "vscode" | "cursor" | "claude-desktop" | "claude-code" | "windsurf" | "m365-copilot"

const PLATFORMS: { id: Platform; label: string; icon: string }[] = [
  { id: "vscode", label: "VS Code", icon: "🆚" },
  { id: "cursor", label: "Cursor", icon: "⊕" },
  { id: "claude-desktop", label: "Claude Desktop", icon: "🤖" },
  { id: "claude-code", label: "Claude Code", icon: "💻" },
  { id: "windsurf", label: "Windsurf", icon: "🌊" },
  { id: "m365-copilot", label: "M365 Copilot", icon: "🪟" },
]

function useCopy() {
  const [copied, setCopied] = useState(false)
  const copy = (text: string) => {
    navigator.clipboard.writeText(text).then(() => {
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    })
  }
  return { copied, copy }
}

function CopyBlock({ label, value }: { label: string; value: string }) {
  const { copied, copy } = useCopy()
  return (
    <div className="space-y-1">
      {label && <p className="text-xs font-medium text-muted-foreground">{label}</p>}
      <div className="flex items-center gap-2 bg-muted rounded-md px-3 py-2 font-mono text-sm break-all">
        <span className="flex-1 text-xs">{value}</span>
        <button
          onClick={() => copy(value)}
          className="shrink-0 text-muted-foreground hover:text-foreground transition-colors"
          aria-label="Copy to clipboard"
        >
          {copied ? <Check className="h-4 w-4 text-green-500" /> : <Copy className="h-4 w-4" />}
        </button>
      </div>
    </div>
  )
}

function PlatformContent({ platform, mcpUrl }: { platform: Platform; mcpUrl: string }) {
  const encodedUrl = encodeURIComponent(mcpUrl)
  const configJson = JSON.stringify(
    { mcpServers: { "agent-atlas": { type: "sse", url: mcpUrl } } },
    null,
    2
  )

  if (platform === "vscode") {
    const deepLink = `vscode://GitHub.copilot-chat/mcp/install?uri=${encodedUrl}`
    return (
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Click the button below to add Agent Atlas to GitHub Copilot in VS Code. Requires the
          GitHub Copilot Chat extension.
        </p>
        <Button asChild className="w-full gap-2">
          <a href={deepLink}>
            <ExternalLink className="h-4 w-4" />
            Add to VS Code (GitHub Copilot)
          </a>
        </Button>
        <p className="text-xs text-muted-foreground">
          Or add manually in VS Code settings under{" "}
          <code className="bg-muted px-1 rounded">github.copilot.chat.mcp.servers</code>:
        </p>
        <CopyBlock label="JSON config snippet" value={`{ "agent-atlas": { "type": "sse", "url": "${mcpUrl}" } }`} />
      </div>
    )
  }

  if (platform === "cursor") {
    const config = JSON.stringify({ "agent-atlas": { url: mcpUrl } })
    const deepLink = `cursor://anysphere.cursor-deeplink/mcp/install?name=agent-atlas&config=${encodeURIComponent(config)}`
    return (
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Click the button below to add Agent Atlas to Cursor, or add the config manually.
        </p>
        <Button asChild className="w-full gap-2">
          <a href={deepLink}>
            <ExternalLink className="h-4 w-4" />
            Add to Cursor
          </a>
        </Button>
        <p className="text-xs text-muted-foreground">
          Or manually add to <code className="bg-muted px-1 rounded">~/.cursor/mcp.json</code>:
        </p>
        <CopyBlock label="mcp.json entry" value={`{ "mcpServers": { "agent-atlas": { "url": "${mcpUrl}" } } }`} />
      </div>
    )
  }

  if (platform === "claude-desktop") {
    return (
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Add the following to your{" "}
          <code className="bg-muted px-1 rounded">claude_desktop_config.json</code> file. On
          macOS:{" "}
          <code className="bg-muted px-1 rounded">
            ~/Library/Application Support/Claude/claude_desktop_config.json
          </code>
        </p>
        <CopyBlock label="claude_desktop_config.json" value={configJson} />
      </div>
    )
  }

  if (platform === "claude-code") {
    const cliCommand = `claude mcp add --transport sse agent-atlas ${mcpUrl}`
    return (
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Run the following command in your terminal to add Agent Atlas to Claude Code:
        </p>
        <CopyBlock label="Terminal command" value={cliCommand} />
        <p className="text-xs text-muted-foreground">
          Requires the{" "}
          <a
            href="https://docs.anthropic.com/en/docs/claude-code"
            target="_blank"
            rel="noreferrer"
            className="underline"
          >
            Claude Code CLI
          </a>{" "}
          to be installed.
        </p>
      </div>
    )
  }

  if (platform === "windsurf") {
    const config = JSON.stringify({ "agent-atlas": { serverUrl: mcpUrl, type: "sse" } })
    const deepLink = `windsurf://mcp/install?name=agent-atlas&config=${encodeURIComponent(config)}`
    return (
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Click the button below to add Agent Atlas to Windsurf, or add the config manually.
        </p>
        <Button asChild className="w-full gap-2">
          <a href={deepLink}>
            <ExternalLink className="h-4 w-4" />
            Add to Windsurf
          </a>
        </Button>
        <p className="text-xs text-muted-foreground">
          Or manually add to{" "}
          <code className="bg-muted px-1 rounded">~/.codeium/windsurf/mcp_config.json</code>:
        </p>
        <CopyBlock
          label="mcp_config.json entry"
          value={`{ "mcpServers": { "agent-atlas": { "serverUrl": "${mcpUrl}", "type": "sse" } } }`}
        />
      </div>
    )
  }

  if (platform === "m365-copilot") {
    return (
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Microsoft 365 Copilot supports MCP servers via{" "}
          <a
            href="https://copilotstudio.microsoft.com"
            target="_blank"
            rel="noreferrer"
            className="underline"
          >
            Microsoft Copilot Studio
          </a>
          . Follow these steps to connect Agent Atlas:
        </p>
        <ol className="space-y-1.5 text-sm text-muted-foreground list-decimal list-inside">
          <li>
            Open{" "}
            <a
              href="https://copilotstudio.microsoft.com"
              target="_blank"
              rel="noreferrer"
              className="underline"
            >
              Copilot Studio
            </a>{" "}
            and create or open an agent.
          </li>
          <li>
            In the agent editor, go to{" "}
            <strong className="text-foreground">Actions</strong> →{" "}
            <strong className="text-foreground">Add an action</strong>.
          </li>
          <li>
            Select <strong className="text-foreground">Model Context Protocol (MCP)</strong>.
          </li>
          <li>Paste the MCP endpoint URL below into the server URL field.</li>
          <li>Save and publish your agent.</li>
        </ol>
        <CopyBlock label="MCP endpoint URL" value={mcpUrl} />
        <p className="text-xs text-muted-foreground">
          Requires a Microsoft 365 Copilot license and admin access to Copilot Studio. See the{" "}
          <a
            href="https://learn.microsoft.com/microsoft-copilot-studio/agent-extend-action-mcp"
            target="_blank"
            rel="noreferrer"
            className="underline"
          >
            Copilot Studio MCP documentation
          </a>{" "}
          for more details.
        </p>
      </div>
    )
  }

  return null
}

export function ConnectMcpPage() {
  const [activePlatform, setActivePlatform] = useState<Platform>("vscode")
  const mcpUrl = `${window.location.origin}/mcp`

  return (
    <div className="space-y-8 max-w-2xl">
      <div>
        <div className="flex items-center gap-2">
          <h1 className="text-3xl font-bold tracking-tight">Connect via MCP</h1>
          <Badge variant="secondary">Agent Atlas</Badge>
        </div>
        <p className="text-muted-foreground mt-1">
          Connect Agent Atlas tools to your AI coding assistant using the Model Context Protocol.
        </p>
      </div>

      <div className="space-y-1">
        <p className="text-xs font-medium text-muted-foreground">MCP Endpoint</p>
        <CopyBlock label="" value={mcpUrl} />
      </div>

      <div className="space-y-3">
        <p className="text-sm font-medium">Add to your tool</p>
        <div className="flex flex-wrap gap-2">
          {PLATFORMS.map((p) => (
            <button
              key={p.id}
              onClick={() => setActivePlatform(p.id)}
              className={
                activePlatform === p.id
                  ? "inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium bg-primary text-primary-foreground"
                  : "inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium border hover:bg-accent hover:text-accent-foreground transition-colors"
              }
            >
              <span>{p.icon}</span>
              {p.label}
            </button>
          ))}
        </div>

        <div className="border rounded-md p-6 bg-muted/30">
          <PlatformContent platform={activePlatform} mcpUrl={mcpUrl} />
        </div>
      </div>
    </div>
  )
}
