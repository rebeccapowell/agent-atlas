import { useState } from "react"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Check, Copy, ExternalLink } from "lucide-react"

interface UseMcpModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

type Platform = "vscode" | "cursor" | "claude-desktop" | "claude-code" | "windsurf"

const PLATFORMS: { id: Platform; label: string; icon: string }[] = [
  { id: "vscode", label: "VS Code", icon: "🆚" },
  { id: "cursor", label: "Cursor", icon: "⊕" },
  { id: "claude-desktop", label: "Claude Desktop", icon: "🤖" },
  { id: "claude-code", label: "Claude Code", icon: "💻" },
  { id: "windsurf", label: "Windsurf", icon: "🌊" },
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
          Or add manually in VS Code settings under <code className="bg-muted px-1 rounded">github.copilot.chat.mcp.servers</code>:
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
          macOS: <code className="bg-muted px-1 rounded">~/Library/Application Support/Claude/claude_desktop_config.json</code>
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
          Requires the <a href="https://docs.anthropic.com/en/docs/claude-code" target="_blank" rel="noreferrer" className="underline">Claude Code CLI</a> to be installed.
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
          Or manually add to <code className="bg-muted px-1 rounded">~/.codeium/windsurf/mcp_config.json</code>:
        </p>
        <CopyBlock label="mcp_config.json entry" value={`{ "mcpServers": { "agent-atlas": { "serverUrl": "${mcpUrl}", "type": "sse" } } }`} />
      </div>
    )
  }

  return null
}

export function UseMcpModal({ open, onOpenChange }: UseMcpModalProps) {
  const [activePlatform, setActivePlatform] = useState<Platform>("vscode")
  const mcpUrl = `${window.location.origin}/mcp`

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <DialogTitle>Use MCP</DialogTitle>
            <Badge variant="secondary" className="text-xs">Agent Atlas</Badge>
          </div>
          <DialogDescription>
            Connect Agent Atlas tools to your AI coding assistant.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-1 mb-1">
          <p className="text-xs font-medium text-muted-foreground">MCP Endpoint</p>
          <CopyBlock label="" value={mcpUrl} />
        </div>

        {/* Platform tabs */}
        <div>
          <p className="text-xs font-medium text-muted-foreground mb-2">Add to your tool</p>
          <div className="flex flex-wrap gap-1">
            {PLATFORMS.map((p) => (
              <button
                key={p.id}
                onClick={() => setActivePlatform(p.id)}
                className={
                  activePlatform === p.id
                    ? "inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md text-xs font-medium bg-primary text-primary-foreground"
                    : "inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md text-xs font-medium border hover:bg-accent hover:text-accent-foreground transition-colors"
                }
              >
                <span>{p.icon}</span>
                {p.label}
              </button>
            ))}
          </div>
        </div>

        <div className="border rounded-md p-4 bg-muted/30">
          <PlatformContent platform={activePlatform} mcpUrl={mcpUrl} />
        </div>
      </DialogContent>
    </Dialog>
  )
}
