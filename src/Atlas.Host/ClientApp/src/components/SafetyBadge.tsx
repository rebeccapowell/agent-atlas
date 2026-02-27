import { Badge } from "@/components/ui/badge"

interface SafetyBadgeProps {
  safety: string
}

export function SafetyBadge({ safety }: SafetyBadgeProps) {
  if (safety === "read") {
    return <Badge variant="success">read</Badge>
  }
  if (safety === "write") {
    return <Badge variant="warning">write</Badge>
  }
  if (safety === "destructive") {
    return <Badge variant="destructive">destructive</Badge>
  }
  return <Badge variant="outline">{safety}</Badge>
}
