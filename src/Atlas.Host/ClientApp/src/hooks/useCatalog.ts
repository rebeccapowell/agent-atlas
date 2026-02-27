import { useState, useEffect } from "react"
import type { ApiEntry, ApiEndpointEntry, ToolDefinition } from "@/types/catalog"

export function useApis() {
  const [apis, setApis] = useState<ApiEntry[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetch("/v1/apis")
      .then((r) => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`)
        return r.json()
      })
      .then(setApis)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false))
  }, [])

  return { apis, loading, error }
}

export function useTools() {
  const [tools, setTools] = useState<ToolDefinition[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetch("/v1/tools")
      .then((r) => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`)
        return r.json()
      })
      .then(setTools)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false))
  }, [])

  return { tools, loading, error }
}

export function useApiEndpoints(apiId: string) {
  const [endpoints, setEndpoints] = useState<ApiEndpointEntry[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setLoading(true)
    setError(null)
    fetch(`/v1/apis/${encodeURIComponent(apiId)}/endpoints`)
      .then((r) => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`)
        return r.json()
      })
      .then(setEndpoints)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false))
  }, [apiId])

  return { endpoints, loading, error }
}
