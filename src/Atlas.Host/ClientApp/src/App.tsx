import { useState } from "react"
import { ApiListPage } from "@/pages/ApiListPage"
import { ToolListPage } from "@/pages/ToolListPage"
import { ThemeToggle } from "@/components/ThemeToggle"
import { BookOpen, Wrench, Activity } from "lucide-react"
import { cn } from "@/lib/utils"

type Page = "apis" | "tools"

function App() {
  const [page, setPage] = useState<Page>("tools")
  const [theme, setTheme] = useState<"light" | "dark">("light")

  const toggleTheme = () => {
    const next = theme === "light" ? "dark" : "light"
    setTheme(next)
    document.documentElement.classList.toggle("dark", next === "dark")
  }

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <header className="border-b sticky top-0 z-40 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="container flex h-16 items-center justify-between">
          <div className="flex items-center gap-2">
            <Activity className="h-6 w-6 text-primary" />
            <span className="font-bold text-lg">Agent Atlas</span>
          </div>

          <nav className="flex items-center gap-1">
            <button
              onClick={() => setPage("apis")}
              className={cn(
                "flex items-center gap-2 px-3 py-2 rounded-md text-sm font-medium transition-colors",
                page === "apis"
                  ? "bg-accent text-accent-foreground"
                  : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
              )}
            >
              <BookOpen className="h-4 w-4" />
              APIs
            </button>
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
            <ThemeToggle theme={theme} onToggle={toggleTheme} />
          </nav>
        </div>
      </header>

      {/* Main */}
      <main className="container py-8">
        {page === "apis" && <ApiListPage />}
        {page === "tools" && <ToolListPage />}
      </main>
    </div>
  )
}

export default App
