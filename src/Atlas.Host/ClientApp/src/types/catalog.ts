export interface ApiEntry {
  apiId: string
  displayName: string
  owner: string
  description: string
  baseUrl: string
  environments: Record<string, string>
}

export interface ApiEndpointEntry {
  method: string
  path: string
  operationId: string
  summary: string
  description: string
  tags: string[]
  isMcpTool: boolean
  toolId?: string
  safety?: string
}

export interface ToolDefinition {
  toolId: string
  apiId: string
  displayName: string
  summary: string
  description: string
  method: string
  path: string
  safety: "read" | "write" | "destructive"
  requiredPermissions: string[]
  tags: string[]
  entitlementHint?: string
  requestSchema?: unknown
  responseSchema?: unknown
  examples?: unknown
  operationId: string
}
