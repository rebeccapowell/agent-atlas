import { useApis, useTools } from "@/hooks/useCatalog"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Loader2, ServerCrash } from "lucide-react"

export function ApiListPage() {
  const { apis, loading: apisLoading, error: apisError } = useApis()
  const { tools } = useTools()

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
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {apis.map((api) => {
          const toolCount = tools.filter((t) => t.apiId === api.apiId).length
          return (
            <Card key={api.apiId}>
              <CardHeader>
                <div className="flex items-start justify-between">
                  <CardTitle className="text-lg">{api.displayName}</CardTitle>
                  <Badge variant="secondary">{toolCount} tool{toolCount !== 1 ? "s" : ""}</Badge>
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
      </div>
    </div>
  )
}
