import { useState } from "react"
import { ApiListPage } from "@/pages/ApiListPage"
import { ApiDetailPage } from "@/pages/ApiDetailPage"
import { ToolListPage } from "@/pages/ToolListPage"
import { ConnectMcpPage } from "@/pages/ConnectMcpPage"
import { AboutPage } from "@/pages/AboutPage"
import { ThemeToggle } from "@/components/ThemeToggle"
import { BookOpen, Wrench, Activity, Zap, Info } from "lucide-react"
import { cn } from "@/lib/utils"

type Page = "apis" | "tools" | "api-detail" | "connect-mcp" | "about"

function App() {
  const [page, setPage] = useState<Page>("tools")
  const [selectedApiId, setSelectedApiId] = useState<string | null>(null)
  const [theme, setTheme] = useState<"light" | "dark">("light")

  const toggleTheme = () => {
    const next = theme === "light" ? "dark" : "light"
    setTheme(next)
    document.documentElement.classList.toggle("dark", next === "dark")
  }

  const navigateToApiDetail = (apiId: string) => {
    setSelectedApiId(apiId)
    setPage("api-detail")
  }

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <header className="border-b sticky top-0 z-40 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="container flex h-16 items-center justify-between">
          <div className="flex items-center gap-3">
            <Activity className="h-6 w-6 text-primary" />
            <div>
              <div className="font-bold text-lg leading-none">Agent Atlas</div>
              <div className="text-xs text-muted-foreground leading-none mt-0.5">MCP Mesh</div>
            </div>
          </div>

          <nav className="flex items-center gap-1">
            <button
              onClick={() => setPage("tools")}
              className={cn(
                "flex items-center gap-2 px-3 py-2 rounded-md text-sm font-medium transition-colors",
                page === "tools"
                  ? "bg-accent text-accent-foreground"
                  : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
              )}
            >
              <Wrench className="h-4 w-4" />
              Tools
            </button>
            <button
              onClick={() => setPage("apis")}
              className={cn(
                "flex items-center gap-2 px-3 py-2 rounded-md text-sm font-medium transition-colors",
                page === "apis" || page === "api-detail"
                  ? "bg-accent text-accent-foreground"
                  : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
              )}
            >
              <BookOpen className="h-4 w-4" />
              APIs
            </button>
            <button
              onClick={() => setPage("connect-mcp")}
              className={cn(
                "flex items-center gap-2 px-3 py-2 rounded-md text-sm font-medium transition-colors",
                page === "connect-mcp"
                  ? "bg-accent text-accent-foreground"
                  : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
              )}
            >
              <Zap className="h-4 w-4" />
              Use MCP
            </button>
            <button
              onClick={() => setPage("about")}
              className={cn(
                "flex items-center gap-2 px-3 py-2 rounded-md text-sm font-medium transition-colors",
                page === "about"
                  ? "bg-accent text-accent-foreground"
                  : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
              )}
            >
              <Info className="h-4 w-4" />
              About
            </button>
            <ThemeToggle theme={theme} onToggle={toggleTheme} />
          </nav>
        </div>
      </header>

      {/* Main */}
      <main className="container py-8">
        {page === "tools" && <ToolListPage />}
        {page === "apis" && <ApiListPage onSelectApi={navigateToApiDetail} />}
        {page === "api-detail" && selectedApiId && (
          <ApiDetailPage apiId={selectedApiId} onBack={() => setPage("apis")} />
        )}
        {page === "connect-mcp" && <ConnectMcpPage />}
        {page === "about" && <AboutPage />}
      </main>
    </div>
  )
}

export default App
