import { useState } from "react"
import { useApis, useTools } from "@/hooks/useCatalog"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Loader2, ServerCrash, Search, ChevronRight } from "lucide-react"

interface ApiListPageProps {
  onSelectApi: (apiId: string) => void
}

export function ApiListPage({ onSelectApi }: ApiListPageProps) {
  const { apis, loading: apisLoading, error: apisError } = useApis()
  const { tools } = useTools()
  const [search, setSearch] = useState("")

  const filtered = apis.filter(
    (a) =>
      !search ||
      a.displayName.toLowerCase().includes(search.toLowerCase()) ||
      a.apiId.toLowerCase().includes(search.toLowerCase()) ||
      a.owner.toLowerCase().includes(search.toLowerCase()) ||
      a.description.toLowerCase().includes(search.toLowerCase())
  )

  if (apisLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (apisError) {
    return (
      <div className="flex flex-col items-center justify-center h-64 gap-2 text-destructive">
        <ServerCrash className="h-8 w-8" />
        <p>Failed to load APIs: {apisError}</p>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">APIs</h1>
        <p className="text-muted-foreground mt-1">
          {apis.length} registered API{apis.length !== 1 ? "s" : ""}
        </p>
      </div>

      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder="Search APIs..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="pl-9"
        />
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {filtered.map((api) => {
          const toolCount = tools.filter((t) => t.apiId === api.apiId).length
          return (
            <Card
              key={api.apiId}
              className="cursor-pointer hover:border-primary transition-colors"
              role="button"
              tabIndex={0}
              onClick={() => onSelectApi(api.apiId)}
              onKeyDown={(e) => {
                if (e.key === "Enter" || e.key === " ") {
                  e.preventDefault()
                  onSelectApi(api.apiId)
                }
              }}
            >
              <CardHeader>
                <div className="flex items-start justify-between">
                  <CardTitle className="text-lg">{api.displayName}</CardTitle>
                  <div className="flex items-center gap-1">
                    <Badge variant="secondary">{toolCount} tool{toolCount !== 1 ? "s" : ""}</Badge>
                    <ChevronRight className="h-4 w-4 text-muted-foreground" />
                  </div>
                </div>
                <CardDescription>{api.owner}</CardDescription>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground">{api.description}</p>
                <div className="mt-2">
                  <span className="text-xs font-mono text-muted-foreground">{api.apiId}</span>
                </div>
              </CardContent>
            </Card>
          )
        })}
        {filtered.length === 0 && (
          <p className="text-muted-foreground col-span-full text-center py-8">
            No APIs found matching &quot;{search}&quot;
          </p>
        )}
      </div>
    </div>
  )
}
