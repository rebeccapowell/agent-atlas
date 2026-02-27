import { useState } from "react"
import { useApis, useApiEndpoints } from "@/hooks/useCatalog"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { SafetyBadge } from "@/components/SafetyBadge"
import { Loader2, ServerCrash, Search, ArrowLeft, Wrench } from "lucide-react"

interface ApiDetailPageProps {
  apiId: string
  onBack: () => void
}

export function ApiDetailPage({ apiId, onBack }: ApiDetailPageProps) {
  const { apis } = useApis()
  const { endpoints, loading, error } = useApiEndpoints(apiId)
  const [search, setSearch] = useState("")

  const api = apis.find((a) => a.apiId === apiId)

  const filtered = endpoints.filter(
    (e) =>
      !search ||
      e.path.toLowerCase().includes(search.toLowerCase()) ||
      e.operationId.toLowerCase().includes(search.toLowerCase()) ||
      e.summary.toLowerCase().includes(search.toLowerCase()) ||
      e.method.toLowerCase().includes(search.toLowerCase())
  )

  const toolCount = endpoints.filter((e) => e.isMcpTool).length

  return (
    <div className="space-y-6">
      {/* Back + header */}
      <div>
        <button
          onClick={onBack}
          className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors mb-4"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to APIs
        </button>

        <div className="flex items-start justify-between gap-4 flex-wrap">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">
              {api?.displayName ?? apiId}
            </h1>
            {api && (
              <p className="text-muted-foreground mt-1">
                {api.owner} · <span className="font-mono text-xs">{api.apiId}</span>
              </p>
            )}
            {api?.description && (
              <p className="text-sm mt-2 text-muted-foreground max-w-2xl">{api.description}</p>
            )}
          </div>
          <div className="flex items-center gap-2 flex-wrap">
            <Badge variant="secondary">
              {endpoints.length} endpoint{endpoints.length !== 1 ? "s" : ""}
            </Badge>
            <Badge variant="outline" className="gap-1">
              <Wrench className="h-3 w-3" />
              {toolCount} MCP tool{toolCount !== 1 ? "s" : ""}
            </Badge>
          </div>
        </div>
      </div>

      {/* Search */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder="Search endpoints..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="pl-9"
        />
      </div>

      {/* Endpoint list */}
      {loading && (
        <div className="flex items-center justify-center h-40">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      )}

      {error && (
        <div className="flex flex-col items-center justify-center h-40 gap-2 text-destructive">
          <ServerCrash className="h-8 w-8" />
          <p>Failed to load endpoints: {error}</p>
        </div>
      )}

      {!loading && !error && (
        <div className="space-y-3">
          {filtered.map((ep) => (
            <Card key={`${ep.method}-${ep.path}`} className={ep.isMcpTool ? "border-primary/40" : undefined}>
              <CardHeader className="pb-2">
                <div className="flex items-start justify-between gap-2 flex-wrap">
                  <CardTitle className="text-sm font-normal flex items-center gap-2 flex-wrap">
                    <Badge variant="outline" className="font-mono text-xs">
                      {ep.method}
                    </Badge>
                    <span className="font-mono">{ep.path}</span>
                    {ep.operationId && (
                      <span className="text-muted-foreground text-xs">{ep.operationId}</span>
                    )}
                  </CardTitle>
                  <div className="flex items-center gap-2 flex-shrink-0">
                    {ep.isMcpTool && ep.safety && <SafetyBadge safety={ep.safety} />}
                    {ep.isMcpTool ? (
                      <Badge className="gap-1 text-xs">
                        <Wrench className="h-3 w-3" />
                        MCP tool
                      </Badge>
                    ) : (
                      <Badge variant="outline" className="text-xs text-muted-foreground">
                        not a tool
                      </Badge>
                    )}
                  </div>
                </div>
              </CardHeader>
              {(ep.summary || ep.isMcpTool) && (
                <CardContent className="pt-0 space-y-1">
                  {ep.summary && (
                    <p className="text-sm text-muted-foreground">{ep.summary}</p>
                  )}
                  {ep.isMcpTool && ep.toolId && (
                    <p className="text-xs font-mono text-muted-foreground">
                      Tool ID: {ep.toolId}
                    </p>
                  )}
                  {ep.tags.length > 0 && (
                    <div className="flex flex-wrap gap-1 pt-1">
                      {ep.tags.map((tag) => (
                        <Badge key={tag} variant="secondary" className="text-xs">{tag}</Badge>
                      ))}
                    </div>
                  )}
                </CardContent>
              )}
            </Card>
          ))}
          {filtered.length === 0 && (
            <p className="text-muted-foreground text-center py-8">
              {search ? `No endpoints matching "${search}"` : "No endpoints found for this API."}
            </p>
          )}
        </div>
      )}
    </div>
  )
}
