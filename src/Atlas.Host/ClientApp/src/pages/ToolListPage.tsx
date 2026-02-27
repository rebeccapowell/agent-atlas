import { useState } from "react"
import { useTools } from "@/hooks/useCatalog"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { SafetyBadge } from "@/components/SafetyBadge"
import { Badge } from "@/components/ui/badge"
import { Loader2, ServerCrash, Search } from "lucide-react"
import type { ToolDefinition } from "@/types/catalog"

interface ToolCardProps {
  tool: ToolDefinition
  onSelect: (tool: ToolDefinition) => void
}

function ToolCard({ tool, onSelect }: ToolCardProps) {
  return (
    <Card
      className="cursor-pointer hover:border-primary transition-colors"
      onClick={() => onSelect(tool)}
    >
      <CardHeader className="pb-2">
        <div className="flex items-start justify-between gap-2">
          <CardTitle className="text-sm font-mono">{tool.toolId}</CardTitle>
          <SafetyBadge safety={tool.safety} />
        </div>
      </CardHeader>
      <CardContent className="space-y-2">
        <p className="text-sm text-muted-foreground">{tool.summary || tool.description}</p>
        <div className="flex items-center gap-2 flex-wrap">
          <Badge variant="outline" className="text-xs font-mono">
            {tool.method} {tool.path}
          </Badge>
          <Badge variant="secondary" className="text-xs">{tool.apiId}</Badge>
        </div>
        {tool.tags.length > 0 && (
          <div className="flex flex-wrap gap-1">
            {tool.tags.map((tag) => (
              <Badge key={tag} variant="outline" className="text-xs">{tag}</Badge>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}

interface ToolDetailProps {
  tool: ToolDefinition
  onClose: () => void
}

function ToolDetail({ tool, onClose }: ToolDetailProps) {
  return (
    <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4" onClick={onClose}>
      <div
        className="bg-background rounded-lg max-w-2xl w-full max-h-[80vh] overflow-y-auto shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="p-6 space-y-4">
          <div className="flex items-start justify-between">
            <div>
              <h2 className="text-xl font-bold">{tool.displayName}</h2>
              <p className="text-sm font-mono text-muted-foreground mt-1">{tool.toolId}</p>
            </div>
            <button
              onClick={onClose}
              className="text-muted-foreground hover:text-foreground text-xl leading-none"
            >
              ×
            </button>
          </div>

          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="font-medium">Method / Path</span>
              <p className="font-mono text-muted-foreground">{tool.method} {tool.path}</p>
            </div>
            <div>
              <span className="font-medium">Safety</span>
              <div className="mt-1"><SafetyBadge safety={tool.safety} /></div>
            </div>
            <div>
              <span className="font-medium">API</span>
              <p className="text-muted-foreground">{tool.apiId}</p>
            </div>
            <div>
              <span className="font-medium">Operation ID</span>
              <p className="font-mono text-muted-foreground">{tool.operationId}</p>
            </div>
          </div>

          {tool.description && (
            <div>
              <span className="text-sm font-medium">Description</span>
              <p className="text-sm text-muted-foreground mt-1">{tool.description}</p>
            </div>
          )}

          {tool.requiredPermissions.length > 0 && (
            <div>
              <span className="text-sm font-medium">Required Permissions (informational)</span>
              <div className="flex flex-wrap gap-1 mt-1">
                {tool.requiredPermissions.map((perm) => (
                  <Badge key={perm} variant="outline" className="text-xs font-mono">{perm}</Badge>
                ))}
              </div>
            </div>
          )}

          {tool.entitlementHint && (
            <div className="bg-muted rounded p-3 text-sm">
              <span className="font-medium">Access Instructions: </span>
              {tool.entitlementHint}
            </div>
          )}

          {tool.tags.length > 0 && (
            <div>
              <span className="text-sm font-medium">Tags</span>
              <div className="flex flex-wrap gap-1 mt-1">
                {tool.tags.map((tag) => (
                  <Badge key={tag} variant="secondary" className="text-xs">{tag}</Badge>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

export function ToolListPage() {
  const { tools, loading, error } = useTools()
  const [search, setSearch] = useState("")
  const [selectedTool, setSelectedTool] = useState<ToolDefinition | null>(null)

  const filtered = tools.filter(
    (t) =>
      !search ||
      t.toolId.toLowerCase().includes(search.toLowerCase()) ||
      t.displayName.toLowerCase().includes(search.toLowerCase()) ||
      t.summary.toLowerCase().includes(search.toLowerCase()) ||
      t.apiId.toLowerCase().includes(search.toLowerCase())
  )

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center h-64 gap-2 text-destructive">
        <ServerCrash className="h-8 w-8" />
        <p>Failed to load tools: {error}</p>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Tools</h1>
          <p className="text-muted-foreground mt-1">
            {tools.length} tool{tools.length !== 1 ? "s" : ""} available
          </p>
        </div>
      </div>

      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder="Search tools..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="pl-9"
        />
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {filtered.map((tool) => (
          <ToolCard key={tool.toolId} tool={tool} onSelect={setSelectedTool} />
        ))}
        {filtered.length === 0 && (
          <p className="text-muted-foreground col-span-full text-center py-8">
            No tools found matching &quot;{search}&quot;
          </p>
        )}
      </div>

      {selectedTool && (
        <ToolDetail tool={selectedTool} onClose={() => setSelectedTool(null)} />
      )}
    </div>
  )
}
